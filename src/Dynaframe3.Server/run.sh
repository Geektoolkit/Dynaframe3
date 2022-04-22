#!/bin/bash

# This file gets run to launch Dynaframe from the autostart command on Linux/Raspberry pi systems
# This is needed to get the 'working directory' synced up. I'm keeping it because I realize it can be used
# to also shim in other fixes without affecting the main codebase which are linux specific before execution, such as turning off
# sleep, or possibly syncing files.
# unclutter is installed by install.sh and used to hide the mouse cursor
# setterm is used to prevent screen blanking
#
# filename/date article on bin/bash/scripting https://www.cyberciti.biz/faq/unix-linux-appleosx-bsd-shell-appending-date-to-filename/
# delete older then 10days on stack overflow https://stackoverflow.com/questions/13489398/delete-files-older-than-10-days-using-shell-script-in-unix

# unclutter -idle 2 & (redundant command that is running elsewhere)
cd /home/pi/DynaframeServer
mkdir -p  /home/pi/DynaframeServer/logs
echo "starting Dynaframe" >> /home/pi/DynaframeServer/logs/run.sh.log

# find and delete any log file older than 10 days, could be longer later.
find /home/pi/DynaframeServer/logs/ -mtime +10 -type f -delete

# set date and time, create log file oneach reboot
now=$(date +"%Y-%m-%d-%H-%M")
export ASPNETCORE_ENVIRONMENT="Production"
./Dynaframe.Server > /home/pi/DynaframeServer/logs/dynaframeserver-${now}.log 2>&1
