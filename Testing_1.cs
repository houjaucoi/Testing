using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Testing
{
    public class Testing_1 : NetworkBehaviour
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

        // Game Logic
        private event Action<ulong, RPS> ClientRPSButtonClicked;

        private RPS playerRPSInput_0 = RPS.None;
        private RPS playerRPSInput_1 = RPS.None;

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
            ClientRPSButtonClicked += HandlePlayerInput;
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
            ClientRPSButtonClicked -= HandlePlayerInput;
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
        public void StartGameLogicServerRpc() 
        {
            playerRPSInput_0 = RPS.None;
            playerRPSInput_1 = RPS.None;

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
        public void RPSButtonOnClickServerRpc(RPS rps, ServerRpcParams serverRpcParams = default)         
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            Debug.Log($"ClientRPSGameInputServerRpc: clientId: {clientId} RPS: {rps}");
            ClientRPSButtonClicked?.Invoke(clientId, rps);
        }

        public void GetRPSGameWinner()
        {
            Debug.Log("GetRPSGameWinner");

            if ((playerRPSInput_0 == RPS.Paper && playerRPSInput_1 == RPS.Paper) || (playerRPSInput_0 == RPS.Rock && playerRPSInput_1 == RPS.Rock) || (playerRPSInput_0 == RPS.Scissors && playerRPSInput_1 == RPS.Scissors))
            {
                Debug.Log("Tie");
                SendGameResultToClient("Tie!", 0);
                SendGameResultToClient("Tie!", 1);
            }
            else if ((playerRPSInput_0 == RPS.Paper && playerRPSInput_1 == RPS.Rock) || (playerRPSInput_0 == RPS.Rock && playerRPSInput_1 == RPS.Scissors) || (playerRPSInput_0 == RPS.Scissors && playerRPSInput_1 == RPS.Paper))
            {
                Debug.Log("player 0 win");
                SendGameResultToClient("You Win!", 0);
                SendGameResultToClient("You Lose!", 1);
            }
            else if ((playerRPSInput_1 == RPS.Paper && playerRPSInput_0 == RPS.Rock) || (playerRPSInput_1 == RPS.Rock && playerRPSInput_0 == RPS.Scissors) || (playerRPSInput_1 == RPS.Scissors && playerRPSInput_0 == RPS.Paper))
            {
                Debug.Log("player 1 win");
                SendGameResultToClient("You Win!", 1);
                SendGameResultToClient("You Lose!", 0);
            }

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
        public void ShowGameResultClientRpc(string content, ClientRpcParams clientRpcParams = default)
        {
            gameContentText.text = content;
        }

        private void HandlePlayerInput(ulong clientId, RPS rps)
        {
            Debug.Log($"ClientId: {clientId}, RPS: {rps}");

            if (clientId == 0)
            {
                playerRPSInput_0 = rps;
            }
            else if(clientId == 1)
            {
                playerRPSInput_1 = rps;
            }
            else
            {
                Debug.Log("???");
            }

            if (playerRPSInput_0 == RPS.None || playerRPSInput_1 == RPS.None) { return; }
            GetRPSGameWinner();
        }
    }
}
