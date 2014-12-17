#!/usr/bin/perl
#This script renames all the asteroids and moons of the main game file.
use XML::Simple;
use Getopt::Long;
use Data::Dumper;
use Scalar::Util qw/reftype/;

my $sandbox_sm   = "Sandbox.sbc";
my $sandbox_lg =  "SANDBOX_0_0_0_.sbs";
my $path; 
my $output;
my $sector = "main";

$result = GetOptions (#"length=i" => \$length,    # numeric
                    "path=s"   => \$path,      # The path to the save file on your harddrive.
					"output=s"	=> \$output,	# This is where the new SANDBOX file is written.
					"sector=s"	=> \$sector,	#This is the name of the sector to call the asteroids
                    "verbose"  => \$verbose);  # flag


my @roids;
my $newroids;
					
#Open the directory
opendir (DIR, $path) or die $!;	

while (my $file = readdir(DIR)) {

	# Use a regular expression to ignore files beginning with a period
	next if ($file =~ m/^\./);
	
	# Only save voxes
	push(@roids, $file) if ($file =~ m/\.vx2/);

	}
closedir(DIR);

#Analyze each roid
foreach my $roid (@roids) {
	
	if($roid !~ m/central/) {
	
		#Extract raw asteroid and moon #
		$roid =~ m/^asteroid(\d+)(?:moon(\d+))?/i;
		
		#Get new name
		my ($asteroid, $moon) = ($1, $2);		
		my $name = "asteroid-$sector-$asteroid";
		
		#Add moon?
		$name .= "-moon-$moon" if defined($moon);
		
		#Add vox
		$name .= ".vx2";
	
		print "$roid will be renamed $name\n";
		$newroids->{$roid} = $name;
	}
	#Main AH asteroid
	elsif($roid eq "centralasteroidb0.vx2")  {
	
		my $name = "asteroid-arrowheads-central.vx2";
	
		print "$roid will be renamed $name\n";
		$newroids->{$roid} = $name;
	}
	#Handle centrals differently.
	else {	
	
		#Extract raw asteroid and moon #
		$roid =~ m/^centralAsteroid(?:moon(\d+))?/i;
		
		#Get new name
		my ($moon) = ($1);		
		my $name = "asteroid-$sector-central";
		
		#Add moon?
		$name .= "-moon-$moon" if defined($moon);
		
		#Add vox
		$name .= ".vx2";
	
		print "$roid will be renamed $name\n";
		$newroids->{$roid} = $name;
	}
}

my $xml = $path."\\SANDBOX_0_0_0_.sbs";

print "Opening $xml\n";

open INXML, "<", $xml or die $!;
open OUTXML, ">", $output or die "$output ".$!;

#Open the XML
while (<INXML>) {

	#Loop for each asteroid, change the XML then the filename.	
	while (($old, $new) = each %{$newroids}) {
	
		my $oldname = substr($old, 0, length($old)-4);
		my $newname = substr($new, 0, length($new)-4);

		if($_ =~ m/\>$oldname\</) {
			print "Renaming $oldname to $newname. ";
			$_ =~ s/$oldname/$newname/g;
			
			print "Moving file ($old) to ($new)\n";
			rename(($path."\\".$old), ($path."\\".$new));
		}
		
	}
	
	print OUTXML $_;
}

close INXML;
close OUTXML;
