 #!/bin/bash
 
version="3.0"


GREEN='\033[1;32m'
CYAN='\033[0;96m'
RED='\033[0;31m'
NC='\033[0m' #no color

while getopts hm:s:f:v: flag
do
    case "${flag}" in
        h) showhelp=true;;
        m) mode=${OPTARG};;
        s) serverurl=${OPTARG};;
        f) frameurl=${OPTARG};;
        v) version=${OPTARG};;
    esac
done

echo -e "${GREEN}=========================================================="
echo -e "  -- Geektoolkit present: Dynaframe 2.0                 --"
echo -e "  -- Preparing to install Version: $version                 --"
echo -e "  -- Find out more on youtube on the ${CYAN}Geektookit ${GREEN}channel --"
echo -e "  -- Created by: Joe Farro  of Geektoolkit              --"
echo -e "  -- Special thanks: qwksilver  RichN001                --"
echo -e "  -- Powered by Avalonia.                               --"
echo -e "==========================================================${NC}"

if [[ $showhelp == true ]];
then
    echo ""
    echo "Usage: $0 {options}"
    echo -e "\t-m Mode"
    echo -e "\t   combined: Will download both the Frame and Server components to be run together"
    echo -e "\t   frame: Will download the Frame only"
    echo -e "\t   server: Will download the Server only"
    echo -e "\t-s Server Url. Will be ignored with Combined Mode as it will use <hostname>:8001"
    echo -e "\t-f Frame Url. Defaults to <hostname>:8000"
    echo -e "\t-v Dynaframe version to use. Optional. Defaults to $version"
    exit 1 # Exit script after printing help
fi

shopt -s nocasematch; if [[ ! "$mode" =~ (frame|server|combined)  ]]; 
then
    echo -e "${RED}Mode must be either frame, server, or combined${NC}"
    exit 1
fi

hostnm=$(hostname)
shopt -s nocasematch; if [[ "$mode" == "combined" ]];
then
    serverurl="http://$hostnm:8001"
fi

if [[ ! $frameurl ]];
then
    frameurl="http://$hostnm:8000"
fi

echo "Cleaning up before we begin..."

#cd /home/pi/
#rm -rf /home/pi/Dynaframe
cd /home/pi/




echo -e "${GREEN}Installing a few tools before we begin (unclutter/unzip) ${NC}"
sudo apt-get install unzip
sudo apt-get install unclutter

# Install the frame
shopt -s nocasematch; if [[ "$mode" == "combined" ]] || [[ "$mode" == "frame" ]]; 
then
    rm -rf /home/pi/.config/autostart/dynaframe.desktop
    rm /home/pi/Dynaframe/*.*
    rm /home/pi/Dynaframe/Dynaframe
    rm /home/pi/Dynaframe/createdump
    rm -rf /home/pi/Dynaframe/images
    mkdir -p  /home/pi/Dynaframe
    cd /home/pi/Dynaframe

    sudo chmod 777 .

    echo -e "${GREEN}Grabbing the frame files from github...hold please..${NC}"

    wget "https://github.com/drinehimer/Dynaframe3/releases/download/$version/Dynaframe2.zip"

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

    echo -e "${GREEN}Adding startup parameters to run.sh"
    sed -i "s/{{urls}}/$(echo $frameurl | sed -r 's/\//\\\//g')/g" run.sh
    sed -i "s/{{serverurl}}/$(echo $serverurl | sed -r 's/\//\\\//g')/g" run.sh

    echo -e "${GREEN}Setting it up to autostart ${NC}"
    mkdir -p /home/pi/.config/autostart
    sudo cp dynaframe.desktop  /home/pi/.config/autostart
    echo -e "${GREEN}cleaning up zip file ${NC}"
    rm Dynaframe2.zip
    echo -e "${GREEN}cleaning up upload dir ${NC}"
    rm /home/pi/Dynaframe/uploads/README.txt
    cp "/home/pi/Dynaframe/install.sh" "/home/pi/UpgradeDynaframe.sh"
    chmod +x /home/pi/UpgradeDynaframe.sh
else
    echo -e "${GREEN}Skipping frame install${NC}"
fi

shopt -s nocasematch; if [[ "$mode" == "combined" ]] || [[ "$mode" == "server" ]]; 
then
    dbFolder="/home/pi/DynaframeServer/Data/"
    tmpDbFolder="/home/pi/Data/"
    if [ -n "$(ls -A $dbFolder 2>/dev/null)" ];
    then
        echo -e "${GREEN}Move database to temporary location${NC}"
        mkdir $tmpDbFolder
        mv $dbFolder* $tmpDbFolder
    fi

    cd /home/pi/
    rm -rf /home/pi/.config/autostart/dynaframeserver.desktop
    cd /home/pi/
    rm -r /home/pi/DynaframeServer
    mkdir -p  /home/pi/DynaframeServer
    cd /home/pi/DynaframeServer

    sudo chmod 777 .

    echo -e "${GREEN}Grabbing the server files from github...hold please..${NC}"

    wget "https://github.com/drinehimer/Dynaframe3/releases/download/$version/DynaframeServer2.zip"

    if [ -f "/home/pi/DynaframeServer/DynaframeServer2.zip" ]
    then
      echo -e "${GREEN}Successfully downloaded archive from Github!${NC}"
    else
      echo -e "${RED}FAILURE! DynaframeServer2.zip did not download successfully ${NC}"
      exit 1
    fi

    echo -e "${GREEN}Unzipping DynaframeServer2.zip ${NC}"
    unzip -u DynaframeServer2.zip
    echo -e "${GREEN}Adding Execution Permissions to DynaframeServer ${NC}"
    sudo chmod +x Dynaframe.Server
    sudo chmod +x run.sh

    echo -e "${GREEN}Adding startup parameters to run.sh"
    sed -i "s/{{serverurl}}/$(echo $serverurl | sed -r 's/\//\\\//g')/g" run.sh

    echo -e "${GREEN}Setting it up to autostart ${NC}"
    mkdir -p /home/pi/.config/autostart
    sudo cp dynaframeserver.desktop  /home/pi/.config/autostart
    echo -e "${GREEN}cleaning up zip file ${NC}"
    rm DynaframeServer2.zip
    
    if [ -n "$(ls -A $tmpDbFolder 2>/dev/null)" ];
    then
        echo -e "${GREEN}Reload database${NC}"
        mv $tmpDbFolder* $dbFolder
        rm -rf $tmpDbFolder
    fi
else
    echo -e "${GREEN}Skipping server install${NC}"
fi