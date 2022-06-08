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
cd /home/pi/Dynaframe
mkdir -p  /home/pi/Dynaframe/logs
echo "starting Dynaframe" >> /home/pi/Dynaframe/logs/run.sh.log

# find and delete any log file older than 10 days, could be longer later.
find /home/pi/Dynaframe/logs/ -mtime +10 -type f -delete

# Make sure that we can reach the server before starting. Give it a little bit as it may be starting up at the same time
#while [[ "$(curl -s -o /dev/null -w ''%{http_code}'' localhost:8081" != "200"]]; 
#do 
#sleep 5; 
#done
secs=180                         
endTime=$(( $(date +%s) + secs ))

echo "Ensuring connection to Dynaframe Server at {{serverurl}}" >> /home/pi/DynaframeServer/logs/run.sh.log
while [ $(date +%s) -lt $endTime ]; do  # Loop until interval has elapsed.
    if [[ "$(curl -s -o /dev/null -w ''%{http_code}'' {{serverurl}}/Heartbeat)" == "200" ]];
    then
        export serverresponded="true"
        break
    else
        sleep 1s
    fi
done

if [[ "$serverresponded" == "true" ]]
then
    # set date and time, create log file oneach reboot
    now=$(date +"%Y-%m-%d-%H-%M")
    ./Dynaframe --urls {{urls}} --dynaframe_server {{serverurl}} > /home/pi/Dynaframe/logs/dynaframe-${now}.log 2>&1
else
    echo "Dynaframe Server did not respond" >> /home/pi/DynaframeServer/logs/run.sh.log
    exit 1
fi