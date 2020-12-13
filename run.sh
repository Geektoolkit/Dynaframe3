#!/bin/bash

# This file gets run to launch Dynaframe from the autostart command on Linux/Raspberry pi systems
# This is needed to get the 'working directory' synced up. I'm keeping it because I realize it can be used
# to also shim in other fixes without affecting the main codebase which are linux specific before execution, such as turning off
# sleep, or possibly syncing files.
# unclutter is installed by install.sh and used to hide the mouse cursor
# setterm is used to prevent screen blanking
#
# filename/date article on bin/bash/scripting https://www.cyberciti.biz/faq/unix-linux-appleosx-bsd-shell-appending-date-to-filename/

unclutter -idle 2 &
cd /home/pi/Dynaframe
mkdir -p  /home/pi/Dynaframe/logs
echo "starting Dynaframe" >> /home/pi/Dynaframe/logs/run.sh.log
./Dynaframe > /home/pi/Dynaframe/logs/dynaframe.log 2>&1
