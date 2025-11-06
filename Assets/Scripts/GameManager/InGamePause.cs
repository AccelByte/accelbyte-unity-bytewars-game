// Copyright (c) 2024 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;

public class InGamePause
{
    public event Action<bool> OnPauseStateChanged;

    private readonly MenuManager menuManager;
    private readonly InGameHUD hud;
    private readonly GameManager gameManager;

    private Stack<MenuCanvas> pauseMenuStack = new Stack<MenuCanvas>();
    
    public bool CanPauseGame()
    {
        return gameManager.InGameState is InGameState.Playing or InGameState.ShuttingDown;
    }
    
    public InGamePause(MenuManager menuManager, InGameHUD hud, GameManager gameManager)
    {
        this.menuManager = menuManager;
        this.hud = hud;
        this.gameManager = gameManager;
        
        GameManager.OnGameStateChanged += GameManagerOnOnGameStateChanged;
    }

    private void GameManagerOnOnGameStateChanged (InGameState newgamestate)
    {
        if (newgamestate == InGameState.GameOver && IsPausing())
        {
            HideAllPauseStack();
        }
    }

    public void HideAllPauseStack()
    {
        while (pauseMenuStack.Count > 0)
        {
            pauseMenuStack.Pop().gameObject.SetActive(false);
        }
    }

    public bool IsPausing()
    {
        if (GameManager.Instance.IsLocalGame)
        {
            return gameManager.InGameState is InGameState.LocalPause;
        }
        return IsOnlineGamePaused();
    }

    private bool IsOnlineGamePaused()
    {
        return pauseMenuStack.Count > 0;
    }
    
    public void ToggleGamePause()
    {
        if (!IsPausing() && !CanPauseGame())
        {
            return;
        }

        if (GameManager.Instance.IsLocalGame)
        {
            ToggleGamePauseLocal();
        }
        else
        {
            ToggleGamePauseOnline();
        }
    }

    public MenuCanvas ShowInGamePauseMenu(AssetEnum assetEnum)
    {
        MenuCanvas lastMenu;
        if (pauseMenuStack.TryPeek(out lastMenu))
        {
            lastMenu.gameObject.SetActive(false);
        }
        MenuCanvas newMenu = menuManager.GetMenu(assetEnum);
        newMenu.gameObject.SetActive(true);
        pauseMenuStack.Push(newMenu);
        return newMenu;
    }

    public void BackToPreviousPauseMenu()
    {
        MenuCanvas lastMenu;
        if (pauseMenuStack.TryPop(out lastMenu))
        {
            lastMenu.gameObject.SetActive(false);
        }
        MenuCanvas currentMenu;
        if (pauseMenuStack.TryPeek(out currentMenu))
        {
            currentMenu.gameObject.SetActive(true);
        }
        else
        {
            // fallback to resume if pauseMenuStack is emptied
            ToggleGamePause();
        }
    }
    
    private void ToggleGamePauseLocal()
    {
        if (gameManager.InGameState is InGameState.LocalPause)
        {
            ResumeLocalGame();
        }
        else if (gameManager.InGameState is InGameState.Playing)
        {
            PauseLocalGame();
        }
        
        OnPauseStateChanged?.Invoke(IsPausing());
    }
    
    private void ResumeLocalGame()
    {
        while (pauseMenuStack.Count > 0)
        {
            pauseMenuStack.Pop().gameObject.SetActive(false);
        }
        gameManager.SetInGameState(InGameState.Playing);
        menuManager.CloseInGameMenu();
    }
    
    private void PauseLocalGame()
    {
        gameManager.SetInGameState(InGameState.LocalPause);
        pauseMenuStack.Push(menuManager.ShowInGameMenu(AssetEnum.PauseMenuCanvas));
    }
    
    private void ToggleGamePauseOnline()
    {
        bool isPausing = IsPausing();
        if (isPausing)
        {
            ResumeOnlineGame();
        }
        else
        {
            PauseOnlineGame();
        }
        
        OnPauseStateChanged?.Invoke(isPausing);
    }
    
    private void ResumeOnlineGame()
    {
        while (pauseMenuStack.Count > 0)
        {
            pauseMenuStack.Pop().gameObject.SetActive(false);
        }
        hud.SetVisible(true);
        menuManager.CloseInGameMenu();
    }
    
    private void PauseOnlineGame()
    {
        hud.SetVisible(false);
        PauseMenuCanvas pauseMenu = menuManager.ShowInGameMenu(AssetEnum.PauseMenuCanvas) as PauseMenuCanvas;
        pauseMenuStack.Push(pauseMenu);

        if (pauseMenu != null)
        {
            pauseMenu.DisableRestartBtn();
        }
    }
}
