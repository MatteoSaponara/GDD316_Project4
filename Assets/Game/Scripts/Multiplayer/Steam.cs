using Mirror;
using Steamworks;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class Steam : SteamManager
    {
        private CallResult<LobbyMatchList_t> m_LobbyMatchList;
        private CallResult<LobbyCreated_t> m_LobbyCreated;
        private CallResult<LobbyEnter_t> m_LobbyEntered;

        private IEnumerator Start()
        {
            // wait for init
            while (!SteamManager.Initialized)
            {
                yield return new WaitForSeconds(1f);
            }

            Debug.Log("Steam Init: " + SteamFriends.GetPersonaName());

            m_LobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
            m_LobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            m_LobbyEntered = CallResult<LobbyEnter_t>.Create(OnLobbyEnetered);

            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);

            yield return new WaitForSeconds(1f);
            SteamAPICall_t requestLobbyAPICall = SteamMatchmaking.RequestLobbyList();

            // Register call to CallResult
            m_LobbyMatchList.Set(requestLobbyAPICall);
        }

        protected override void Update()
        {
            base.Update();
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                SteamAPICall_t handle = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 10);
                m_LobbyCreated.Set(handle);
            }
        }

        private void OnLobbyMatchList(LobbyMatchList_t callback, bool bIOFailure)
        {
            if (bIOFailure) return;

            for (int i = 0; i < callback.m_nLobbiesMatching; i++)
            {
                CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);

                // These functions get common data which all lobbies have
                int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
                int maxMembers = SteamMatchmaking.GetLobbyMemberLimit(lobbyId);

                // GetLobbyData allows you to fetch custom lobby data, often used for version numbers, etc.
                string lobbyName = SteamMatchmaking.GetLobbyData(lobbyId, "name");
                string bobcat = SteamMatchmaking.GetLobbyData(lobbyId, "bobcat");
                if (!string.IsNullOrEmpty(bobcat))
                {
                    SteamAPICall_t call = SteamMatchmaking.JoinLobby(lobbyId);
                    Debug.Log("Joining lobby: " + lobbyName);
                    return;
                }

                Debug.Log($"{lobbyName} - {memberCount}/{maxMembers}");
            }
        }

        private void OnLobbyCreated(LobbyCreated_t callback, bool bIOFailure)
        {
            if (bIOFailure || callback.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogError($"Failed to create lobby. Result: {callback.m_eResult}");
                return;
            }

            CSteamID lobbyID = new CSteamID(callback.m_ulSteamIDLobby);
            Debug.Log($"Lobby created successfully! Lobby ID: {lobbyID}");

            // Set the custom display name
            SteamMatchmaking.SetLobbyData(lobbyID, "name", SteamFriends.GetPersonaName() + "'s Lobby");

            // Required for Mirror + FizzySteamworks: Store host's Steam 64-bit ID so joiners can connect
            string hostSteamID = SteamUser.GetSteamID().ToString();
            SteamMatchmaking.SetLobbyData(lobbyID, "HostAddress", hostSteamID);

            // Add custom "bobcat" data to find Bobcat servers
            SteamMatchmaking.SetLobbyData(lobbyID, "bobcat", "Yes");

            // Start Host
            NetworkManager.singleton.StartHost();
        }

        private void OnLobbyEnetered(LobbyEnter_t callback, bool bIOFailure)
        {
            if (bIOFailure)
            {
                Debug.LogError("Join lobby fail");
                return;
            }
            CSteamID activeLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            Debug.Log($"Entered lobby: {activeLobbyId}");

            // get host address to join game
            string hostAddress = SteamMatchmaking.GetLobbyData(activeLobbyId, "HostAddress");
        }
    }
}