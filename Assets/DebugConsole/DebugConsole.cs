// Copyright (c) 2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections;
using System.Threading.Tasks;
using AccelByte.Api;
using AccelByte.Core;
using AccelByte.Server;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Debugger
{
    public class DebugConsole : MonoBehaviour
    {

        [SerializeField]
        private DraggableBtn debuggerBtn;
        [SerializeField]
        private Transform container;
        [SerializeField]
        private DebugButtonItem btnPrefab;
        [SerializeField]
        private GameObject logScrollView;
        [SerializeField]
        private Text logText;
        [SerializeField]
        private ContentSizeFitter contentSizeFitter;
        [SerializeField]
        private MaxActivePanel maxActiveSessionPanel;


        private static DebugConsole _instance = null;
        private Lobby lobby;
#if UNITY_SERVER
        private ServerDSHub serverDSHub;
        private ServerSession serverSession;
#endif

        public static DebugConsole Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<DebugConsole>("DebugConsole");
                    _instance = Instantiate(_instance);
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
        }

        internal void Start()
        {
            debuggerBtn.SetClickCallback(ClickDebugger);
            AddButton("close", Close);
            AddButton("destroy debugger", Destroy);
            AddButton("toggle log", SwitchLogVisibility);
            AddButton("clear log", ClearLog);
            AddButton("disconnect lobby", DisconnecLobby);
#if UNITY_SERVER
            AddButton("disconnect dshub", DisconnecDSHub);
            AddButton("reconcile max active session", ReconcileMaxActive);
            AddButton("check user max active", CheckUserMaxActive);
#endif


            lobby = AccelByteSDK.GetClientRegistry().GetApi().GetLobby();
#if UNITY_SERVER
            serverDSHub = AccelByteSDK.GetServerRegistry().GetApi().GetDsHub();
            serverSession = AccelByteSDK.GetServerRegistry().GetApi().GetSession();
#endif
        }

        private void Destroy()
        {
            Destroy(gameObject);
        }

        private void Close()
        {
            container.gameObject.SetActive(false);
        }

        private void ClickDebugger()
        {
            container.gameObject.SetActive(true);
        }

        private void SwitchLogVisibility()
        {
            logScrollView.SetActive(!logScrollView.activeSelf);
            if(logScrollView.activeSelf)
            {
                Instance.StartCoroutine(waitOneFrame(() => { Instance.contentSizeFitter.enabled = true; }));
            }
        }
        private void OnReceivedMsg(string logString, string stackTrace, LogType type)
        {
            Log(logString);
        }

        private void ClearLog()
        {
            logText.text = "";
            contentSizeFitter.enabled = false;
            StartCoroutine(waitOneFrame(() => { Instance.contentSizeFitter.enabled = true; }));
        }

        private void DisconnecLobby()
        {
            lobby.Disconnect();
        }

#if UNITY_SERVER

        private void DisconnecDSHub()
        {
            serverDSHub.Disconnect();
            Log("Disconnected");
        }

        private void ReconcileMaxActive()
        {
            maxActiveSessionPanel.gameObject.SetActive(true);
            maxActiveSessionPanel.Title.text = "Reconcile Max Active Session";
            TMP_Text resultText = maxActiveSessionPanel.LogResult.GetComponentInChildren<TMP_Text>();
            string playerId = maxActiveSessionPanel.PlayerIdInput.text;
            string sessionConfiguration = maxActiveSessionPanel.SessionConfigurationInput.text;
            maxActiveSessionPanel.TriggerButton.GetComponentInChildren<TMP_Text>().text = "Reconcile";
            maxActiveSessionPanel.TriggerButton.onClick.AddListener(() =>
            {   
                maxActiveSessionPanel.TriggerButton.onClick.RemoveAllListeners();
                serverSession.ReconcileMaxActiveSession(playerId, sessionConfiguration, result => 
                {
                    if (!result.IsError)
                    {
                        resultText.color = Color.green;
                        string log = $"Successfully reconcile max active session : {!result.IsError}";
                        Log(log);
                        resultText.text = log;
                    }
                    else
                    {
                        resultText.color = Color.red;
                        Log($"{result.Error.Message}");
                        resultText.text = result.Error.Message;
                    }
                });
            });
        }
        
        private void CheckUserMaxActive()
        {
            
            maxActiveSessionPanel.gameObject.SetActive(true);
            maxActiveSessionPanel.Title.text = "Check User Max Active Session";
            TMP_Text resultText = maxActiveSessionPanel.LogResult.GetComponentInChildren<TMP_Text>();
            string playerId = maxActiveSessionPanel.PlayerIdInput.text;
            string sessionConfiguration = maxActiveSessionPanel.SessionConfigurationInput.text;
            maxActiveSessionPanel.TriggerButton.GetComponentInChildren<TMP_Text>().text = "Check MaxActiveSession";
            maxActiveSessionPanel.TriggerButton.onClick.AddListener(() =>
            {   
                maxActiveSessionPanel.TriggerButton.onClick.RemoveAllListeners();
                serverSession.GetMemberActiveSession(playerId, sessionConfiguration, result => 
                {
                    if (!result.IsError)
                    {
                        resultText.color = Color.green;
                        Log($"User Max Active Session {result.Value.Total}");
                        resultText.text = result.Value.ToJsonString();
                    }
                    else
                    {
                        resultText.color = Color.red;
                        Log($"{result.Error.Message}");
                        resultText.text = result.Error.Message;
                    }
                });
            });
        }

#endif
        public static void AddButton(string btnLabel, UnityAction callback)
        {
#if BYTEWARS_DEBUG
            DebugButtonItem localButton = Instantiate(Instance.btnPrefab, Instance.container, false);
            localButton.SetBtn(btnLabel, callback);
            localButton.name = btnLabel;
#endif
        }

        private static void Log(string text)
        {
#if BYTEWARS_DEBUG
            Instance.logText.text += text + '\n';
            Instance.contentSizeFitter.enabled = false;
            Instance.StartCoroutine(waitOneFrame(() => { Instance.contentSizeFitter.enabled = true; }));
            //Debug.Log(text);
#endif
        }

        static IEnumerator waitOneFrame(Action callback)
        {
            yield return new WaitForEndOfFrame();
            if(callback!=null)
            {
                callback();
            }
        }
    }
}
