GREEN='\033[1;32m'
CYAN='\033[0;96m'
NC='\033[0m' #no color

echo -e "${GREEN} Geektoolkit present: Dynaframe 2.0 ${NC}"
echo "Cleaning up before we begin..."
rm -rf /home/pi/Dynaframe
rm -rf /home/pi/.config/autostart/dynaframe.desktop
sudo apt-get install unzip

mkdir -p  /home/pi/Dynaframe
cd Dynaframe
sudo chmod 777 .
echo -e "${GREEN}Grabbing the files from github...hold please..${NC}"
wget https://github.com/Geektoolkit/Dynaframe3/releases/download/2.03/Dynaframe203.zip
echo "Unzipping them!"
unzip Dynaframe203.zip
echo -e "${GREEN}Adding Execution Permissions to Dynaframe ${NC}"
sudo chmod +x Dynaframe
sudo chmod +x run.sh
echo -e "${GREEN}Setting it up to autostart ${NC}"
mkdir -p /home/pi/.config/autostart
sudo cp dynaframe.desktop  /home/pi/.config/autostart
echo -e "${GREEN}cleaning up zip file ${NC}"
rm Dynaframe203.zip



