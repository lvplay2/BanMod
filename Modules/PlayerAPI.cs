using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace BanMod
{
    public static class PlayerAPI
    {
        private const string ApiUrl = "https://banmod.online/api/player_status";
        private const int RetryDelay = 120; // 2 minuti in secondi

        public static IEnumerator SendPlayerStatusCoroutine(string playerName, string friendCode, string gameCode, string region, string language, int playersInLobby, bool isOnline, bool shareLobby)
        {
            string json = $"{{\"PlayerName\":\"{EscapeJson(playerName)}\",\"FriendCode\":\"{EscapeJson(friendCode)}\",\"GameCode\":\"{EscapeJson(gameCode)}\",\"Region\":\"{EscapeJson(region)}\",\"Language\":\"{EscapeJson(language)}\",\"PlayersInLobby\":{playersInLobby},\"IsOnline\":{isOnline.ToString().ToLower()},\"ShareLobby\":{shareLobby.ToString().ToLower()}}}";

            bool sentSuccessfully = false;

            while (!sentSuccessfully)
            {
                UnityWebRequest www = new UnityWebRequest(ApiUrl, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                // Verifica se c'è un errore di rete o HTTP
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    // Log l'errore e aspetta 5 minuti
                    Debug.LogError($"Errore durante la connessione: {www.error}. Riproverò tra {RetryDelay / 60} minuti...");
                    yield return new WaitForSeconds(RetryDelay);
                }
                else
                {
                    // Risposta riuscita, esci dal ciclo
                    sentSuccessfully = true;
                }

                www.Dispose();
            }
        }

        public static IEnumerator SendMinimalPlayerStatusCoroutine(string friendCode, bool isOnline, bool sharelobby)
        {
            string json = $"{{\"FriendCode\":\"{EscapeJson(friendCode)}\",\"IsOnline\":{isOnline.ToString().ToLower()},\"ShareLobby\":{sharelobby.ToString().ToLower()}}}";

            bool sentSuccessfully = false;

            while (!sentSuccessfully)
            {
                UnityWebRequest www = new UnityWebRequest(ApiUrl, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                // Verifica se c'è un errore di rete o HTTP
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    // Log l'errore e aspetta 5 minuti
                    Debug.LogError($"Errore durante la connessione: {www.error}. Riproverò tra {RetryDelay / 60} minuti...");
                    yield return new WaitForSeconds(RetryDelay);
                }
                else
                {
                    // Risposta riuscita, esci dal ciclo
                    sentSuccessfully = true;
                }

                www.Dispose();
            }
        }

        public static IEnumerator SendMinimalPlayerStatusCoroutine1(string friendCode, bool isOnline)
        {
            string json = $"{{\"FriendCode\":\"{EscapeJson(friendCode)}\",\"IsOnline\":{isOnline.ToString().ToLower()}}}";

            bool sentSuccessfully = false;

            while (!sentSuccessfully)
            {
                UnityWebRequest www = new UnityWebRequest(ApiUrl, "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                // Verifica se c'è un errore di rete o HTTP
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    // Log l'errore e aspetta 5 minuti
                    Debug.LogError($"Errore durante la connessione: {www.error}. Riproverò tra {RetryDelay / 60} minuti...");
                    yield return new WaitForSeconds(RetryDelay);
                }
                else
                {
                    // Risposta riuscita, esci dal ciclo
                    sentSuccessfully = true;
                }

                www.Dispose();
            }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";

            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
    }
}