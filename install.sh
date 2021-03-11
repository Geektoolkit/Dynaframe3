 #!/bin/bash
version=$1
file="/home/pi/Dynaframe/appsettings.json"

GREEN='\033[1;32m'
CYAN='\033[0;96m'
RED='\033[0;31m'
NC='\033[0m' #no color

echo "Version is: $version"
if [ -z "$1" ]
then
  echo -e  "${CYAN}No version passed in...using default${NC}"
  version="2.18"
fi


echo -e "${GREEN}=========================================================="
echo -e "  -- Geektoolkit present: Dynaframe 2.0                 --"
echo -e "  -- Preparing to install Version: $version                 --"
echo -e "  -- Find out more on youtube on the ${CYAN}Geektookit ${GREEN}channel --"
echo -e "  -- Created by: Joe Farro  of Geektoolkit              --"
echo -e "  -- Special thanks: qwksilver  RichN001                --"
echo -e "  -- Powered by Avalonia.                               --"
echo -e "==========================================================${NC}"
if [ -f "$file" ];
then
  echo -e "${GREEN} Found old appsettings..backing up ${NC}"
  cp $file "/home/pi/"
fi

echo "Cleaning up before we begin..."

#cd /home/pi/
#rm -rf /home/pi/Dynaframe
cd /home/pi/
rm -rf /home/pi/.config/autostart/dynaframe.desktop
cd /home/pi/
rm /home/pi/Dynaframe/*.*
rm /home/pi/Dynaframe/Dynaframe
rm /home/pi/Dynaframe/createdump
rm -rf /home/pi/Dynaframe/web/ico
rm /home/pi/Dynaframe/web/*.*
rm -rf /home/pi/Dynaframe/web/css
rm -rf /home/pi/Dynaframe/web/js
rm -rf /home/pi/Dynaframe/web/images
rm -rf /home/pi/Dynaframe/images




echo -e "${GREEN}Installing a few tools before we begin (unclutter/unzip) ${NC}"
sudo apt-get install unzip
sudo apt-get install unclutter
mkdir -p  /home/pi/Dynaframe
cd Dynaframe

sudo chmod 777 .

echo -e "${GREEN}Grabbing the files from github...hold please..${NC}"

wget "https://github.com/Geektoolkit/Dynaframe3/releases/download/$version/Dynaframe2.zip"

if [ -f "/home/pi/Dynaframe/Dynaframe2.zip" ]
then
  echo -e "${GREEN}Successfully downloaded archive from Github!${NC}"
else
  echo -e "${RED}FAILURE! Dynaframe2.zip did not download successfully ${NC}"
  exit 1
fi

echo -e "${GREEN}Unzipping Dynaframe2.zip ${NC}"
unzip -u Dynaframe2.zip
echo -e "${GREEN}Adding Execution Permissions to Dynaframe ${NC}"
sudo chmod +x Dynaframe
sudo chmod +x run.sh
echo -e "${GREEN}Setting it up to autostart ${NC}"
mkdir -p /home/pi/.config/autostart
sudo cp dynaframe.desktop  /home/pi/.config/autostart
echo -e "${GREEN}cleaning up zip file ${NC}"
rm Dynaframe2.zip
echo -e "${GREEN}cleaning up upload dir ${NC}"
rm /home/pi/Dynaframe/web/uploads/README.txt
if [ -f "/home/pi/appsettings.json" ];
then
  echo -e "${GREEN}Restoring old appsettings...${NC}"
  cd /home/pi/
  cp "appsettings.json" "/home/pi/Dynaframe"
fi
cp "/home/pi/Dynaframe/install.sh" "/home/pi/UpgradeDynaframe.sh"
chmod +x /home/pi/UpgradeDynaframe.sh

