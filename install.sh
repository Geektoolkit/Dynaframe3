 #!/bin/bash
version=$1
file="/home/$USER/Dynaframe/appsettings.json"

GREEN='\033[1;32m'
CYAN='\033[0;96m'
RED='\033[0;31m'
NC='\033[0m' #no color

echo "Version is: $version"
if [ -z "$1" ]
then
  echo -e  "${CYAN}No version passed in...using default${NC}"
  version="2.24"
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
  cp $file "/home/$USER/"
fi

echo "Cleaning up before we begin..."

#cd /home/$USER/
#rm -rf /home/$USER/Dynaframe
cd /home/$USER/
rm -rf /home/$USER/.config/autostart/dynaframe.desktop
cd /home/$USER/
rm /home/$USER/Dynaframe/*.*
rm /home/$USER/Dynaframe/Dynaframe
rm /home/$USER/Dynaframe/createdump
rm -rf /home/$USER/Dynaframe/web/ico
rm /home/$USER/Dynaframe/web/*.*
rm -rf /home/$USER/Dynaframe/web/css
rm -rf /home/$USER/Dynaframe/web/js
rm -rf /home/$USER/Dynaframe/web/images
rm -rf /home/$USER/Dynaframe/images




echo -e "${GREEN}Installing a few tools before we begin (unclutter/unzip) ${NC}"
sudo apt-get install unzip
sudo apt-get install unclutter
mkdir -p  /home/$USER/Dynaframe
cd Dynaframe

sudo chmod 777 .

echo -e "${GREEN}Grabbing the files from github...hold please..${NC}"

wget "https://github.com/Geektoolkit/Dynaframe3/releases/download/$version/Dynaframe2.zip"

if [ -f "/home/$USER/Dynaframe/Dynaframe2.zip" ]
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
mkdir -p /home/$USER/.config/autostart
sudo cp dynaframe.desktop  /home/$USER/.config/autostart
echo -e "${GREEN}cleaning up zip file ${NC}"
rm Dynaframe2.zip
echo -e "${GREEN}cleaning up upload dir ${NC}"
rm /home/$USER/Dynaframe/web/uploads/README.txt
if [ -f "/home/$USER/appsettings.json" ];
then
  echo -e "${GREEN}Restoring old appsettings...${NC}"
  cd /home/$USER/
  cp "appsettings.json" "/home/$USER/Dynaframe"
fi
cp "/home/$USER/Dynaframe/install.sh" "/home/$USER/UpgradeDynaframe.sh"
chmod +x /home/$USER/UpgradeDynaframe.sh
