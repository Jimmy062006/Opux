# Opux
Opux is a Discord bot for EVEOnline

This bot is currently under development and needs to run with dotnet SDK/Runtime. The following guide is a very short setup description. Complete documentation following soon™.

# Features
* Price check tool
* Auth
* Admin Reauth
* Radius Killmails
* Fleetup Operations Post
* MOTD Pulling to channel
* EVE Time Mod

For downloading and installing dotnet core, follow the guide below and make sure you use the SDK:
https://www.microsoft.com/net/core

After the installation, clone the repository with

> git clone https://github.com/Jimmy062006/Opux.git

When it's done pulling go into the `Opux/src/Opux` and run 

> dotnet restore

When this is done, modify the settingsfile (settings.new.json) within the /src/ folder. 
Afterwards rename it to `settings.json` and copy it into `Opux/src/Opux/bin/Debug/netcoreapp1.1/`. 

Linux

> To bring up your bot run `screen` and navigate to Opux/src/Opux and
> start it with `dotnet run`. Now detach the screen (Ctrl + A , D) and
> see your bot coming alive.

Windows

> To bring up your bot run `powershell "dotnet run | tee -a Log.txt` and
> see your bot coming alive.

For any further questions contact us on discord:

https://discord.gg/KX5Wrkj
