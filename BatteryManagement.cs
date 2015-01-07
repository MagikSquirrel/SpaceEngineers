//http://forums.keenswh.com/post/power-management-script-make-your-batteries-work-the-way-you-always-wanted-them-to-7228518
/*Special thanks to the Reactor Usage script created by "me 10 Jin" which I have blatantly ripped REGEX usage from
  And also to Digi, whose Airlock Cycling script was used to figure out the timers for solar power output updating.
*/  
//regex   taken from 10Jin's Reactor Usage script
System.Text.RegularExpressions.Regex pwrRegex = new System.Text.RegularExpressions.Regex(      
"Max Output: (\\d+\\.?\\d*) (\\w?)W.*Current Output: (\\d+\\.?\\d*) (\\w?)W"      
, System.Text.RegularExpressions.RegexOptions.Singleline);   

//regex altered from the above, this will detect stored and available power on a battery.
System.Text.RegularExpressions.Regex batteryRegex = new System.Text.RegularExpressions.Regex(       
"Max Stored Power: (\\d+\\.?\\d*) (\\w?)Wh.*Stored power: (\\d+\\.?\\d*) (\\w?)Wh"       
, System.Text.RegularExpressions.RegexOptions.Singleline);    
 
System.Timers.Timer cooldownTimer;
//This line can be used to enable automatic looping without a timer.
//System.Timers.Timer restartTimer;
  
void Main()  
{  
    //want battery set to charge if solar panels are outputting enough excess power to charge them.  
    //want battery set to discharge if solar panels are not outputting enough excess power(to save uranium)  
    //(stretch) enable reactors if batteries are all set to "discharge" and still not producing enough power.  
        //batteries should never charge from reactors.

    //time to wait after shutting batteries down to check solar panels.
    int holdTime = 100;
    //without this delay, the solar panels won't update their output fast enough.
    //100ms is sufficient, less might work.

    //get all batteries and turn them off.
    var batteries = new List<IMyTerminalBlock>();   
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries);    
  
    //disable all batteries before performing this check.  
    for(int i=0;i<batteries.Count;i++)  
    {  
        batteries[i].GetActionWithName("OnOff_Off").Apply(batteries[i]);  
    }
    //wait for power system to normalize
    cooldownTimer = new System.Timers.Timer(holdTime);    
    cooldownTimer.Elapsed += new System.Timers.ElapsedEventHandler(TimedEvent);   
    cooldownTimer.Start();  
  
    return;  
}  
  
void TimedEvent(object source, System.Timers.ElapsedEventArgs e)    
{  
    //halt timer  
    cooldownTimer.Stop();  
  
    //Grab all solar panels and batteries on the grid.   
    var solars = new List<IMyTerminalBlock>();    
    GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(solars);   
  
    var batteries = new List<IMyTerminalBlock>();  
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries);   
   
    double pwrMaxSum = 0.0;   
    double pwrNowSum = 0.0;   
    double solarPowerRatio = 0.0;   
    double batteryPowerRatio = 0.0;

    //These lines are to grab antennas for text ouptut. 
    //Don't use them if you don't want your antennas getting randomly renamed. 
//    var antennas = new List<IMyTerminalBlock>();   
//    GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(antennas);   
  
    //Get total solar power ratio(thanks again 10Jin)  
    for ( int i = 0; i <solars.Count ; i++ )   
    {   
        System.Text.RegularExpressions.Match match = pwrRegex.Match( solars[i].DetailedInfo );  
        Double n;  
        if ( match.Success )   
        {   
            if ( Double.TryParse( match.Groups[1].Value, out n ) )   
                pwrMaxSum += n * Math.Pow( 1000.0, ".kMGTPEZY".IndexOf( match.Groups[2].Value ) );

            if ( Double.TryParse( match.Groups[3].Value, out n ) )    
                pwrNowSum += n * Math.Pow( 1000.0, ".kMGTPEZY".IndexOf( match.Groups[4].Value ) );
        }   
    }
    //make sure any solar panels were found.  
    if(pwrMaxSum>0.0)  
    {  
        solarPowerRatio = pwrNowSum/pwrMaxSum;  
        //antennas[0].SetCustomName("Solar power usage ratio: " + solarPowerRatio);  
    }  
    else  
    {  
        solarPowerRatio = 9999;  
        //antennas[0].SetCustomName("Error: no solar energy being produced.");
    }  
    if(solarPowerRatio < 0.99)//approximate cutoff due to floating-point weirdness  
    {  
        //Grab all reactors, turn them off(to save uranium)  
        var reactors = new List<IMyTerminalBlock>();    
        GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactors);  
        for(int i=0;i<reactors.Count;i++)  
        {  
            reactors[i].GetActionWithName("OnOff_Off").Apply(reactors[i]);  
        }  
        //grab all batteries, set them to "charge."  
        for(int i=0;i<batteries.Count;i++)   
        {   
            //turn battery on because it was turned off for the solar check.  
            batteries[i].GetActionWithName("OnOff_On").Apply(batteries[i]);  
            bool recharge = true;  
            //Fun fact: Batteries don't have a field to tell you whether they're in "charge" mode or not!
            //Fortunately, the DetailedInfo string will tell you how long it will take to recharge or discharge.
            //checking which of these messages is in use will let us know whether we're
            //charging or discharging.
            string batteryInfo = batteries[i].DetailedInfo;  
            recharge = batteryInfo.Contains("recharged");  
            //if discharging
            if(!recharge)  
            {  
                batteries[i].GetActionWithName("Recharge").Apply(batteries[i]);  
            }  
        }  
    }  
    else//battery power needed.  
    {  
        for(int i=0;i<batteries.Count;i++)    
        {    
            //turn battery on because it was turned off for the solar check.   
            batteries[i].GetActionWithName("OnOff_On").Apply(batteries[i]);   
            bool recharge = true;   
            //Check whether we're currently in recharge mode
            string batteryInfo = batteries[i].DetailedInfo;  
            recharge = batteryInfo.Contains("recharged");  
            //if it is, switch it out of recharge mode.  
            if(recharge)   
            {   
                batteries[i].GetActionWithName("Recharge").Apply(batteries[i]);   
            }   
        }  
        //check battery current/max  
        for ( int i = 0; i <batteries.Count ; i++ )    
        { 
            //filter out batteries that are empty from being counted among supply. 
            IMyBatteryBlock thisBattery = batteries[i] as IMyBatteryBlock; 
            if(thisBattery.HasCapacityRemaining) 
            { 
                System.Text.RegularExpressions.Match match = batteryRegex.Match( batteries[i].DetailedInfo );
                Double n;

                if ( match.Success ) 
                { 
                    //check if battery is "empty."(<0.5% charge) 
                    Double pwrMaxStored = 0.0; 
                    Double pwrNowStored = 0.0; 
                    Double pwrStoredPercent = 0.0;  

                    //Seriously, thanks "me 10 Jin." I couldn't have done this without your example.
                    if ( Double.TryParse( match.Groups[1].Value, out n ) )  
                        pwrMaxStored = n * Math.Pow( 1000.0, ".kMGTPEZY".IndexOf( match.Groups[2].Value ) );  
   
                    if ( Double.TryParse( match.Groups[3].Value, out n ) )    
                        pwrNowStored += n * Math.Pow( 1000.0, ".kMGTPEZY".IndexOf( match.Groups[4].Value ) ); 
 
                    pwrStoredPercent = pwrNowStored/pwrMaxStored*100;
                    //antennas[0].SetCustomName("Battery is at " + pwrStoredPercent + " percent of capacity.");
 
                    if(pwrStoredPercent > 0.5) 
                    {
                        //there's enough battery power to be usable, check the output.
                        match = batteryRegex.Match( batteries[i].DetailedInfo );
                        if(match.Success)
                        {
                            if ( Double.TryParse( match.Groups[1].Value, out n ) )  
                                pwrMaxSum += n * Math.Pow( 1000.0, ".kMGTPEZY".IndexOf( match.Groups[2].Value ) );  
 
                            if ( Double.TryParse( match.Groups[3].Value, out n ) )   
                                pwrNowSum += n * Math.Pow( 1000.0, ".kMGTPEZY".IndexOf( match.Groups[4].Value ) );   
                        }
                    } 
                }  
            }  
        } 
        //if THAT's above 0.99, turn on the reactors.  
        if(pwrMaxSum > 0.0)  
        {  
            batteryPowerRatio = pwrNowSum/pwrMaxSum;  
        }  
        else  
        {  
            batteryPowerRatio = 9999;  
        }  
        if(batteryPowerRatio > 0.99)  
        {  
            var reactors = new List<IMyTerminalBlock>();     
            GridTerminalSystem.GetBlocksOfType<IMyReactor>(reactors);   
            for(int i=0;i<reactors.Count;i++)   
            {   
                reactors[i].GetActionWithName("OnOff_On").Apply(reactors[i]);   
            }  
        }  
    }  
}
