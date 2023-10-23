# Extreal.Integration.Chat.WebRTC

## How to test

- Enter the following command in the `WebScripts~` directory.

   ```bash
   yarn
   yarn dev
   ```

- Import the sample MVS from Package Manager.
- Enter the following command in the `MVS/WebScripts` directory.

   ```bash
   yarn
   yarn dev
   ```

   The JavaScript code will be built and output to `/Assets/WebTemplates/Dev`.
- Open `Build Settings` and change the platform to `WebGL`.
- Select `Dev` from `Player Settings > Resolution and Presentation > WebGL Template`.
- Add all scenes in MVS to `Scenes In Build`.
- See [README](https://github.com/extreal-dev/Extreal.Integration.P2P.WebRTC/SignalingServer~/README.md) to start a signaling server.
- Play
  - Native
    - Open multiple Unity editors using ParrelSync.
    - Run
      - Scene: MVS/App/App
  - WebGL
    - See [README](https://github.com/extreal-dev/Extreal.Dev/blob/main/WebGLBuild/README.md) to run WebGL application in local environment.

## Test cases for manual testing

### Host

- Group selection screen
  - Ability to create a group by specifying a name (host start)
- VirtualSpace
  - Client can join a group (client join)
  - Clients can leave the group (client exit)
  - Ability to send text (text chat)
  - Ability to talk (voice chat)
  - Ability to return to the group selection screen (host stop)

### Client

- Group selection screen
  - Ability to join a group (join host)
- Virtual space
  - Ability to send text (text chat)
  - Ability to talk (voice chat)
  - Ability to return to the group selection screen while moving the player (leave host)
