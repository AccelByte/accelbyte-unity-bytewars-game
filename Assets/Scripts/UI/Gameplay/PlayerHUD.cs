using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [System.Serializable]
    public class Column
    {
        public TextMeshProUGUI m_title;
        public TextMeshProUGUI m_value;
    }
    
    [SerializeField]
    private Column[] m_columns;

    [SerializeField] private Canvas _canvas;

    enum ColumnEnum
    {
        Lives,
        Score,
        Kills
    }
    

    public void SetColour(Color colour)
    {
        foreach(Column column in m_columns)
        {
            column.m_title.color = colour;
            column.m_value.color = colour;
        }
    }

    public void SetLivesValue(int livesValue)
    {
        m_columns[(int)ColumnEnum.Lives].m_value.text = livesValue.ToString();
    }

    public void SetScoreValue(int scoreValue)
    {
        m_columns[(int)ColumnEnum.Score].m_value.text = scoreValue.ToString();
    }

    public void SetKillsValue(int killsValue)
    {
        m_columns[(int)ColumnEnum.Kills].m_value.text = killsValue.ToString();
    }

    public void Reset()
    {
        foreach(Column column in m_columns)
        {
            column.m_title.color = Color.white;
            column.m_value.color = Color.white;
            column.m_value.text = "0";
        }
        _canvas.enabled = false;
    }

    public void Init(TeamState teamState, PlayerState[] pStates)
    {
        SetColour(teamState.teamColour);
        int lives = 0;
        foreach (var pState in pStates)
        {
            if (pState.TeamIndex == teamState.teamIndex)
            {
                lives += pState.Lives;
            }
        }
        SetLivesValue(lives);
        SetScoreValue(0);
        SetKillsValue(0);
        _canvas.enabled = true;
    }
    
    

}
