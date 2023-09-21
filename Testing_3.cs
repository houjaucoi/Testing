using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Testing
{
    public class Testing_3 : NetworkBehaviour
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

        private AwaitableCompletionSource<RPS> playerRPSInput_0 = new AwaitableCompletionSource<RPS>();
        private AwaitableCompletionSource<RPS> playerRPSInput_1 = new AwaitableCompletionSource<RPS>();

        private Awaitable<RPS> WaitForClientRPSInputAsync_0() => playerRPSInput_0.Awaitable;
        private Awaitable<RPS> WaitForClientRPSInputAsync_1() => playerRPSInput_1.Awaitable;

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

            if (clientId == 0)
            {
                playerRPSInput_0.SetResult(rps);
            }
            else if (clientId == 1)
            {
                playerRPSInput_1.SetResult(rps);
            }
        }

        public void GetRPSGameWinner(RPS clientRPSInput_0, RPS clientRPSInput_1)
        {
            Debug.Log("GetRPSGameWinner");

            if ((clientRPSInput_0 == RPS.Paper && clientRPSInput_1 == RPS.Paper) || (clientRPSInput_0 == RPS.Rock && clientRPSInput_1 == RPS.Rock) || (clientRPSInput_0 == RPS.Scissors && clientRPSInput_1 == RPS.Scissors))
            {
                Debug.Log("Tie");
                SendGameResultToClient("Tie!", 0);
                SendGameResultToClient("Tie!", 1);
            }
            else if ((clientRPSInput_0 == RPS.Paper && clientRPSInput_1 == RPS.Rock) || (clientRPSInput_0 == RPS.Rock && clientRPSInput_1 == RPS.Scissors) || (clientRPSInput_0 == RPS.Scissors && clientRPSInput_1 == RPS.Paper))
            {
                Debug.Log("player 0 win");
                SendGameResultToClient("You Win!", 0);
                SendGameResultToClient("You Lose!", 1);
            }
            else if ((clientRPSInput_1 == RPS.Paper && clientRPSInput_0 == RPS.Rock) || (clientRPSInput_1 == RPS.Rock && clientRPSInput_0 == RPS.Scissors) || (clientRPSInput_1 == RPS.Scissors && clientRPSInput_0 == RPS.Paper))
            {
                Debug.Log("player 1 win");
                SendGameResultToClient("You Win!", 1);
                SendGameResultToClient("You Lose!", 0);
            }

            playerRPSInput_0.Reset();
            playerRPSInput_1.Reset();

            startGameButton.interactable = true;
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

            var clientRPSInput_0 = await WaitForClientRPSInputAsync_0();
            var clientRPSInput_1 = await WaitForClientRPSInputAsync_1();

            if (clientRPSInput_0 == RPS.None || clientRPSInput_1 == RPS.None) { return; }

            GetRPSGameWinner(clientRPSInput_0, clientRPSInput_1);
        }
    }
}