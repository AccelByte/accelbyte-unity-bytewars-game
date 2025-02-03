# Byte Wars AMS - Unity

## Overview

Byte Wars AMS is the official tutorial game for AccelByte Multiplayer Servers (AMS). It is intended to act as a sample project that can be used as a reference for the best practices to integrate AMS features into your game. The purpose of Byte Wars AMS tutorials are to guide you on how to host your game server on AMS quickly without the need to integrate AccelByte Gaming Services (AGS) in your game. This way, you can still use any kind of your backend services to manage your game sessions while AMS will handle the dedicated servers management.

## Prerequisites

- **Unity Editor 2022.3.50f1** installed on your computer.
- The sample custom matchmaking backend service from [AMS Samples](https://github.com/AccelByte/ams-samples) repository.
- An **AGS game namespace**, either AGS Shared Cloud or AGS Private Cloud.
- Basic knowledge on how to use **Git**.

## Clone Byte Wars

You can find the **Byte Wars AMS** project from the `main-ams` branch on the [Byte Wars Unity GitHub repository](https://github.com/AccelByte/accelbyte-unity-bytewars-game/tree/main-ams). Use the Git command below to clone the project.

```bash
git clone -b main-ams https://github.com/AccelByte/accelbyte-unity-bytewars-game.git
```

The `main-ams` branch contain [AccelByte Unity SDK](https://github.com/AccelByte/accelbyte-unity-sdk) as project dependencies defined in the `Packages/manifest.json` file.

## Open Byte Wars in Unity

1. Launch the **Unity Hub**. From the side panel, select **Projects** and click on the **Add** button. From the dropdown, select the **Add project from disk**.
2. On the pop-up window, locate the **Byte Wars** project you cloned and click ont he **Add Project** button.
3. Once done, the **Byte Wars** project will be listed on the **Unity Hub**. Make sure the **EDITOR VERSION** is set to **2022.3.50f1**.
4. Click the project entry from the list to open it on the Unity Editor.

## Run Byte Wars

### Game Client

#### Run via Unity Editor

1. Open **Byte Wars** project in Unity Editor. Then, go to the **Assets/Scenes** folder and double-click the **MainMenu.unity** scene file to open the Main Menu scene.
2. Click on the play button to start the game in the Unity Editor.

#### Run via Packaged Game

1. Open **Byte Wars** project in Unity Editor. 
2. Specifically for WebGL build, you need to enable compression and data caching to optimize the packaged WebGL build. To do this, go to **File > Edit > Project Settings**. In the new window, select **Player** from the left panel, then select **Settings for WebGL** tab. Next, expand the **Publishing Settings** section and set **Compression Format** to **Brotli** and enable **Data Caching**.
3. To package and build the game, go to **File > Build Settings**.
4. On the **Build Settings** window, use the following settings:
    - For Windows Build: select **Windows, Mac, Linux** for the **Platform** and choose **Windows** as the **Target Platform**.
    - Fow WebGL Build: select **WebGL** for the **Platform**.
5. Then, click on the **Build** button to build your game.
6. In the file manager window, choose the folder where you want to save the game build file and click **Select Folder** button.
7. Unity will start to build and package your game. Once it is done, open the folder where you saved the game build. To run the game, follow the instruction below:
    - For Windows Build: run the `ByteWars.exe` to start the game client.
    - For WebGL Build: host the game on your web server and run the game by opening your web server's IP address in a web browser.

### Game Server

#### Run via Packaged Server

1. Open **Byte Wars** project in Unity Editor. 
3. To package and build the game server, go to **File > Build Settings**.
4. On the **Build Settings** window, use the following settings:
    - For Windows Build: select **Dedicated Server** for the **Platform** and choose **Windows** as the **Target Platform**.
    - For Linux Build: select **Dedicated Server** for the **Platform** and choose **Windows** as the **Target Platform**.
5. Then, click on the **Build** button to build your game server.
6. In the file manager window, choose the folder where you want to save the game server build file and click **Select Folder** button.
7. Unity will start to build and package your game server. Once it is done, open the folder where you saved the game server build. To run the game server, follow the instruction below:
    - For Windows Build: run the `ByteWars.exe` to start the game server.
    - For Linux Build: run the `ByteWars.x86_64` to start the game server.

### Build from Command Line

1. Build Client run: `build_client.bat`
2. Build Server run: `build_server.bat`

### Enable debug mode

1. Open the project in Unity Editor. 
2. Go to **Edit > Project Settings > Player > Script Compilation > Scripting Define Symbols**. 
3. Then, add `BYTEWARS_DEBUG` symbol to enable debug mode. 

## How to connect the game to the sample custom matchmaking backend service

1. Run the sample custom matchmaking backend service on your local computer.
2. Next, locate the game client build and then open Windows PowerShell on the game client build folder. 
3. On the Windows PowerShell, enter the command below to run your game client. The `-CustomMatchmakingUrl=` launch parameter tells the game client to use sample backend service to perform matchmaking. If you run the service locally, you can set the value to `ws://127.0.0.1:8080`.

    ```bash
    ./ByteWars.exe -CustomMatchmakingUrl=ws://<your_service_ip>:<your_service_port>
    ```

4. Once the game client starts, navigate to **Custom Matchmaking** and click on the **Start Matchmaking** button to start matchmaking. The game client will connect to your backend service via WebSocket.

## Byte Wars AMS integration tutorial
Follow along [Byte Wars AMS](https://docs.accelbyte.io/gaming-services/tutorials/byte-wars-ams/unity/) to learn more about the integration.

## Git
To revert files with only line ending difference use --renormalize. Example this command will revert all prefabs with only line ending difference and also stage (add) other actually changed prefab files. 
```
git add --renormalize *.prefab
```
