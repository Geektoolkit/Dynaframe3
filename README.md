# Dynaframe 2
Dynamic Photo and Video Slideshow system for SBC (such as Raspberry pi)

Video Demo, how to install, and intro here: https://youtu.be/XEaUsaNEzjY

Note: Please see the releases section for the latest release. The below command will install 2.07 which I consider stable, but there are more features in later releases if you'd like to try them. To remove a release just delete the folder and rerun the install script.

Quickstart: On a raspberry pi based system you'll want to connect to a network, put a few images in the Pictures folder to get started, and then run:

    sudo wget https://github.com/Geektoolkit/Dynaframe3/releases/download/2.07/install.sh && bash install.sh; rm -f install.sh ; sudo reboot
    
On reboot it should startup, and it'll show you the URL of the configuration page.

To exit, please hit the 'esc' button.  

To restart on a raspberry pi, you can launch it again using ./Dynaframe 

Hardware Requirements:
I recommend a Raspberry pi 3B+ or better. It really shines on a Pi4 2gig or better.
A heatsink is required...this gets VERY CPU intensive when transitioning, and fast transitions will thermal the pi and bring up the temp gauge.
I recommend either a FLIRC style case, a good heatsink solution, or a small fan for active cooling.  
Adding memory to the GPU and slowing down the time between transitions (as well as speeding up the transition time) can help with cooling if needed.

Additional steps to clean up some things:
1) To get rid of the mouse cursor, you'll want to install unclutter.  sudo apt-get install unclutter, and then run: unclutter -idle 0
2) To prevent screen blanking, please run: sudo raspi-config, and then turn off screen blanking in the menus there (I believe it's under advanced)
I'm working on getting these into the setup scripts.

Welcome to Dynaframe!  Dynaframe was designed to be a simple photo and video slideshow viewer.  I wanted to have something that could do slideshows but also show 'plotagraphs', which are essentially animated images.  I use an app on iOS called 'Werble' which made some really cool ones, but there wasn't a good way to show them off.

The first version of Dynaframe was simply a python script that went through a folder list and then executed feh or omxplayer base don the file extension.  This was buggy, unstable, ugly, and in the end I abandoned it, however I had a ton of requests for updates and features. I decided to rewrite it in Avalonia, and that has proven to make things MUCH better.  The feature set increased dramatically, and every time I get a few hours to work on it I add a few more.  currently there are:
Features:
1) crossfades - Image to image will now crossfade (vs. v1 where they simply appeared)
2) Fullscreen - You no longer have to hide the task bar and black out the background...this will automaticlaly take up the full screen
3) persistent settings - Changes made via the web UI will now be saved
4) Rotation! - You no longer have to rotate the OS, you can rotate on the fly and save the setting for the slideshow (per image rotation is not supported)
5) All photos - instead of being limited to photos in a single folder, all photos in your 'my pictures' folder or equivalent will now be able to be selected
6) Scaling - If your image doens't take up the full screen, it'll now automatically be zoomed to fill the frame
7) Smaller Download size - V1 was a 16 gig Raspbian image, this is under 30 megs!
8) No need to hide the background - This was something that had to be done in V1. No longer! The background is covered by the app
9) Easier 'autostart' setup - This now sets up the 'run on startup' for you as part of the installer
10) Uses pictures folder - This allows for more updates without having to worry about losing your slideshow/setups. It also is easier to get pictures into the pictures folder than a proprietary one
11) Shutdown from webui - You can now shut the frame off! Since the frame may not have a keyboard/mouse hooked up to it, being able to safely shut it off is handy
12) custom slide times - You can now, without editing code, select the time you want photos to be on the frame between transitions
13) custom transition times - If you want a quick cross fade, or a looong slow one, you can now do that.  I find that 30 second crossfades are VERY cool effects..try it!
14) Filetype support - It's very easy to now add image and video type supports. This already supports more files than V1, and I'll keep adding more! For videos MPEG, MP4, MOV, AVI are all supported
15) Filename fixes - V1 had a very buggy situation with filenames...underscores, dashes, and other characters would wreck it.  In this version this is all fixed!
16) Clock - You can now turn on a digtal clock display, and even set the font size
17) Ease of use improvements - Besides all of the stuff above, it now shows the IP/port of the webserver when it's started, so that it's easy for users to load the settings page on thier phone
18) Web frontend for settings - A simple web frontend is supplied so that settings can be loaded easily. They persist.  And being a simple webpage means no app to install, and works on just about any device
19) Playlists - You can put pictures in folders and play them as 'playlists' which allows you to sort your pictures and show them in ways that usually aren't possible when using standard slideshow software (without restarting everything)


Question: What happened to Dynaframe 2? 
  Dynaframe 2 was a WIP that I never shipped. I worked on it using the pi3D library, and just never got it off the ground
  
  Q: What's next for this project?
    I have a few 'hot' features I want to get in.  These include:
    1) EXIF data - I think having the ability to mark up images with data such as where its taken, or the story behind it, would be handy, esp. if being used as a family photo frame.  I want to have a way to show that data
    2) DONE - font settings - I'd like to have the front for the clock, and future uses, have settings
    3) Weather/RSS feeds - There is the ability to easily turn on text...it'd be useful to have that get other data such as possibly a stock quote, weather, calendar info, or an RSS feed
    4) Gesture/keypad/ir remotes - I'd like to have support for methods to control the panel, for instance an IR Remote could let someone enter thier wifi account info possibly
    5) customizable keys - I'd like to enable keyboard shortcuts for those that do have keyboards connevcted, and to start then shimming in Gestures and other control methods built on top of that
 
 
 Compiling:
   This is built on Avalonia in VS 2019, and the dotnet core SDK.  The dependancies should come down via Nuget.
 
 Thankyou for taking a look at my project. To simply install it on a raspbery pi, you can launch one of the following (if you're not installing on a pi, you'll likely have to compile it for now. I'll make differnet releases based on demand.

(This one autoreboots at the end)
sudo wget https://github.com/Geektoolkit/Dynaframe3/releases/download/2.03/install.sh && bash install.sh; rm -f install.sh ; sudo reboot

If you want to keep the install.sh around to poke at it and not reboot, you can also try:
sudo wget https://github.com/Geektoolkit/Dynaframe3/releases/download/2.03/install.sh && bash install.sh

Thankyou. Please file issues as you find them and I'll do what I can. This is a side project for me.


