//https://stackoverflow.com/questions/12451609/how-to-await-raising-an-eventhandler-event

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Testing
{
    public class Testing_2 : NetworkBehaviour
    {
        [Header("Panel")]
        public GameObject gamePanel;

        [Header("Button")]
        public Button hostButton;
        public Button clientButton;
        public Button startGameButton;
        public Button leaveGameButton;

        [Header("Text")]
        public TMP_Text playerIdText;
        public TMP_Text gameContentText;

        // GameLogic
        public event Action<ulong, RPS> ClientRPSButtonClicked;

        public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);
        public event AsyncEventHandler<EventArgs> AsyncEvent;

        public void Start()
        {
            gamePanel.SetActive(false);
        }

        public override void OnNetworkSpawn()
        {
            playerIdText.text = $"PlayerId: {NetworkManager.Singleton.LocalClientId}";

            hostButton.interactable = false;
            clientButton.interactable = false;
            leaveGameButton.interactable = true;

            if (!IsServer) { return; }
            startGameButton.interactable = true;
        }

        public override void OnNetworkDespawn()
        {
            gamePanel.SetActive(false);

            playerIdText.text = string.Empty;
            gameContentText.text = string.Empty;

            hostButton.interactable = true;
            clientButton.interactable = true;

            leaveGameButton.interactable = false;
            startGameButton.interactable = false;

            if (!IsServer) { return; }
        }

        public void StartHost()
        {
            NetworkManager.Singleton.StartHost();
        }

        public void StartClient()
        {
            NetworkManager.Singleton.StartClient();
        }

        public void LeaveGame()
        {
            NetworkManager.Singleton.Shutdown();
        }

        public void StartGame()
        {
            if (NetworkManager.Singleton.ConnectedClientsList.Count < 2)
            {
                Debug.Log($"Current player count: {NetworkManager.Singleton.ConnectedClientsList.Count}");
                gameContentText.text = "Player count = 1";
                Invoke(nameof(ResetGameContentText), 2f);
                return;
            }

            startGameButton.interactable = false;
            StartGameLogicServerRpc();
        }

        public void ResetGameContentText()
        {
            gameContentText.text = string.Empty;
        }

        [ServerRpc]
        private void StartGameLogicServerRpc()
        {
            _ = GameLogic();
            ShowGamePanelClientRpc();
        }

        [ClientRpc]
        public void ShowGamePanelClientRpc()
        {
            gameContentText.text = string.Empty;
            gamePanel.SetActive(true);
        }

        public void RPSGameButtonOnClick(int i)
        {
            Debug.Log($"RPSGameButtonOnClick: {(RPS)i}");

            gameContentText.text = $"You selected {(RPS)i}, waiting for opponent...";

            RPSButtonOnClickServerRpc((RPS)i);
            gamePanel.SetActive(false);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RPSButtonOnClickServerRpc(RPS rps, ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            Debug.Log($"ClientRPSGameInputServerRpc: clientId: {clientId} RPS: {rps}");
            ClientRPSButtonClicked?.Invoke(clientId, rps);
        }

        public void GetRPSGameWinner() 
        {
            Debug.Log("GetRPSGameWinner");
        }

        public void SendGameResultToClient(string content, ulong clientId)
        {
            ShowGameResultClientRpc(content, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            });
        }

        [ClientRpc]
        private void ShowGameResultClientRpc(string content, ClientRpcParams clientRpcParams = default)
        {
            gameContentText.text = content;
        }

        private async Task GameLogic() 
        {
            Debug.Log("Start GameLogic");

            (ulong, RPS) testing = await HandlePlayerInputdAsync();
            Debug.Log($"{testing.Item1}, {testing.Item2}");
        }

        private async Task<(ulong, RPS)> HandlePlayerInputdAsync()         
        {
            Debug.Log("Start HandlePlayerInputdAsync");

            var tcs = new TaskCompletionSource<(ulong, RPS)>();

            try
            {
                var result = await tcs.Task;
                return result;
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            return (Convert.ToUInt64(-1), RPS.None);
        }
    }
}

//public class Testing_2 : NetworkBehaviour
//{
//    public event EventHandler<RPS> playerRPSInput;

//    public async Task<RPS> GetPlayerRPSInputAsync()
//    {
//        var tcs = new TaskCompletionSource<RPS>();
//        EventHandler<RPS> lamda = (s, e) => tcs.TrySetResult(RPS.None);

//        try
//        {
//            playerRPSInput += lamda;
//            var result = await tcs.Task;
//            return result;
//        }
//        catch (Exception e)
//        {
//            Debug.Log(e);
//        }
//        finally
//        {
//            playerRPSInput -= lamda;
//        }

//        return RPS.None;
//    }
//}
