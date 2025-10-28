# 3D TicTacToe Multiplayer & AI in Unity

## Overview
- The game board and pieces are 3D prefabs instantiated in the scene.
- UI Toolkit is used only for menus, status, and mode selection.
- Two game modes: Singleplayer (vs AI) and Multiplayer (Player vs Player).
- AI is only present in singleplayer mode.

## Todo List

1. **Create 3D board and cell prefabs**
   - Design a 3x3 grid using 3D objects.
   - Each cell is a clickable object.

2. **Create X and O 3D piece prefabs**
   - Model or extrude meshes for X and O.
   - Prefabs are instantiated at cell positions.

3. **Implement GameStateManager with NetworkVariable for board/turn**
   - Store board state (int[9]) and current turn.
   - Use NetworkVariable for multiplayer sync.

4. **Implement ServerRpc for move validation and prefab instantiation**
   - Validate moves on the server.
   - Instantiate X/O prefabs on all clients.

5. **Assign player symbols (X/O) based on connection order**
   - Use NetworkManager to determine which client is X or O.

6. **Implement AI logic (random/minimax) for singleplayer only**
   - AI makes moves for the other side in singleplayer.
   - Trigger AI move after player’s turn.

7. **Set up UI Toolkit for menus/status/rematch**
   - Main menu for mode selection.
   - Status display for current turn, winner, or draw.
   - Rematch and exit buttons.

8. **Connect UI events to game logic**
   - Button clicks trigger moves or menu actions.

9. **Handle networking setup (host/client, disconnects, rematch)**
   - Multiplayer uses Netcode for GameObjects.
   - Manage player connections and rematches.

10. **Test all modes and edge cases**
    - Ensure correct sync, win/draw detection, AI moves, disconnects, and rematch functionality.

## Game Mode Logic

- **Singleplayer:**
  - Only one client.
  - Player is X (or O).
  - AI makes moves for the other side.
  - GameStateManager triggers AI move after player’s turn.

- **Multiplayer:**
  - Two clients.
  - Each player takes turns.
  - No AI logic.

## References

- [Netcode for GameObjects Docs](https:docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest)
- [UI Toolkit Docs](https:docs.unity3d.com/Manual/UIElements.html)
- [UI Toolkit Sample Projects](https:assetstore.unity.
com/packages/essentials/tutorial-projects/dragon-crashers-ui-toolkit-sample-project-231178)