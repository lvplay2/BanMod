using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static BanMod.Translator;

namespace BanMod
{
    [HarmonyPatch]
    public class ModUpdater
    {
        private static readonly string URL = "https://api.github.com/repos/GianniBart/BanMod";
        public static bool hasUpdate = false;
        public static bool isBroken = false;
        public static bool isChecked = false;
        public static Version latestVersion = null;
        public static string latestTitle = null;
        public static string downloadUrl = null;
        public static GenericPopup InfoPopup;

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.LowerThanNormal)]
        public static void StartPostfix()
        {
            DeleteOldDLL();
            InfoPopup = UnityEngine.Object.Instantiate(Twitch.TwitchManager.Instance.TwitchPopup);
            InfoPopup.name = "InfoPopup";
            InfoPopup.TextAreaTMP.GetComponent<RectTransform>().sizeDelta = new(2.5f, 2f);

            if (!isChecked)
            {
                CheckRelease().GetAwaiter().GetResult();
            }
            if (hasUpdate && !string.IsNullOrEmpty(downloadUrl))
            {
                ShowUpdateAvailablePopup();
            }
            // Mostra sempre il pulsante
            MainMenuManagerPatch.UpdateButton.gameObject.SetActive(true);

            // Rimuove eventuali listener precedenti
            MainMenuManagerPatch.UpdateButton.OnClick = new();
            MainMenuManagerPatch.UpdateButton.OnClick.AddListener((Action)(() =>
            {
                if (hasUpdate && !string.IsNullOrEmpty(downloadUrl))
                {
                    StartUpdate(downloadUrl);
                }
                else
                {
                    ShowPopup(GetString("noUpdateAvailable"), true, false); // Mostra solo il messaggio, non esce
                }
            }));
        }
        public static async Task<bool> CheckRelease(bool beta = false)
        {
            string url = URL + "/releases/latest";
            try
            {
                string result;
                using (HttpClient client = new())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "BanMod Updater");
                    using var response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseContentRead);
                    if (!response.IsSuccessStatusCode || response.Content == null)
                    {
                        return false;
                    }
                    result = await response.Content.ReadAsStringAsync();
                }
                JObject data = JObject.Parse(result);
                {
                    latestVersion = new(data["tag_name"]?.ToString().TrimStart('v'));
                    latestTitle = $"Ver. {latestVersion}";
                    JArray assets = data["assets"].Cast<JArray>();
                    for (int i = 0; i < assets.Count; i++)
                    {
                        if (assets[i]["name"].ToString() == "BanMod.dll")
                            downloadUrl = assets[i]["browser_download_url"].ToString();
                    }
                    hasUpdate = latestVersion.CompareTo(BanMod.version) > 0;
                }
                if (downloadUrl == null)
                {
                    return false;
                }
                isChecked = true;
                isBroken = false;
            }
            catch (Exception)
            {
                isBroken = true;
                return false;
            }
            return true;
        }
        public static void StartUpdate(string url)
        {
            ShowPopup(GetString("updatePleaseWait"));
            if (!BackupDLL())
            {
                ShowPopup(GetString("updateManually"), true);
                return;
            }
            _ = DownloadDLL(url);
            return;
        }
        public static bool BackupDLL()
        {
            try
            {
                File.Move(Assembly.GetExecutingAssembly().Location, Assembly.GetExecutingAssembly().Location + ".bak");
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static void DeleteOldDLL()
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.bak"))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
            return;
        }
        public static async Task<bool> DownloadDLL(string url)
        {
            try
            {
                using HttpClient client = new();
                using var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    using var content = response.Content;
                    using var stream = await content.ReadAsStreamAsync();
                    using var file = new FileStream("BepInEx/plugins/BanMod.dll", FileMode.Create, FileAccess.Write);
                    await stream.CopyToAsync(file);

                    //  Mostra popup e abilita chiusura/redirect
                    ShowPopup(GetString("updateRestart"), true, true);
                    return true;
                }
            }
            catch (Exception)
            {
                // Log errore se vuoi
            }

            ShowPopup(GetString("updateManually"), true, false);
            return false;
        }
        private static void DownloadCallBack(object sender, DownloadProgressChangedEventArgs e)
        {
            ShowPopup($"{GetString("updateInProgress")}\n{e.BytesReceived}/{e.TotalBytesToReceive}({e.ProgressPercentage}%)");
        }
        private static void ShowPopup(string message, bool showButton = false, bool exitApp = false)
        {
            if (InfoPopup != null)
            {
                InfoPopup.Show(message);

                // Rimuove eventuale pulsante "UpdateLabel"
                var updateBtn = InfoPopup.transform.Find("UpdateLabel");
                if (updateBtn != null)
                {
                    UnityEngine.Object.Destroy(updateBtn.gameObject);
                }

                var button = InfoPopup.transform.Find("ExitGame");
                if (button != null)
                {
                    // Ripristina posizione e scala se modificati in precedenza
                    button.localPosition = Vector3.zero;
                    button.localScale = Vector3.one;

                    button.gameObject.SetActive(showButton);

                    var txt = button.GetComponentInChildren<TMP_Text>(true);
                    var translator = txt.GetComponent<TextTranslatorTMP>();
                    if (translator != null) UnityEngine.Object.Destroy(translator);
                    txt.enableWordWrapping = false;
                    txt.fontSize = 3.5f;

                    txt.text = GetString("CloseLabel");

                    button.GetComponent<PassiveButton>().OnClick = new();
                    button.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
                    {
                        if (exitApp)
                        {
                            Application.OpenURL("https://github.com/GianniBart/BanMod/releases/latest");
                            Application.Quit();
                        }
                        else
                        {
                            InfoPopup.gameObject.SetActive(false);
                        }
                    }));
                }
            }
        }
        private static void ShowUpdateAvailablePopup()
        {
            if (InfoPopup != null)
            {
                InfoPopup.Show($"{GetString("updateAvailable")}\n{latestTitle}");

                var closeBtn = InfoPopup.transform.Find("ExitGame");
                if (closeBtn == null) return;

                // Clona il pulsante "ExitGame"
                var updateBtn = UnityEngine.Object.Instantiate(closeBtn, closeBtn.parent);
                updateBtn.name = "UpdateLabel";

                // Sposta e ridimensiona i pulsanti
                updateBtn.localPosition = closeBtn.localPosition + new Vector3(-0.7f, 0, 0);
                updateBtn.localScale = new Vector3(0.85f, 1f, 1f);
                updateBtn.gameObject.SetActive(true);

                closeBtn.localPosition = updateBtn.localPosition + new Vector3(+1.4f, 0, 0);
                closeBtn.localScale = new Vector3(0.85f, 1f, 1f);
                closeBtn.gameObject.SetActive(true);

                // --- Aggiorna il testo del pulsante "Aggiorna ora"
                var updateText = updateBtn.GetComponentInChildren<TMP_Text>(true);
                var updateTranslator = updateText.GetComponent<TextTranslatorTMP>();
                if (updateTranslator != null) UnityEngine.Object.Destroy(updateTranslator);

                updateText.enableWordWrapping = false;
                updateText.fontSize = 3.5f;
                updateText.text = GetString("UpdateLabel"); // oppure: "Aggiorna ora"

                // Azione pulsante "Aggiorna ora"
                var updateButtonComponent = updateBtn.GetComponent<PassiveButton>();
                updateButtonComponent.OnClick = new();
                updateButtonComponent.OnClick.AddListener((Action)(() =>
                {
                    StartUpdate(downloadUrl);
                }));

                // --- Aggiorna il testo del pulsante "Chiudi"
                var closeText = closeBtn.GetComponentInChildren<TMP_Text>(true);
                var closeTranslator = closeText.GetComponent<TextTranslatorTMP>();
                if (closeTranslator != null) UnityEngine.Object.Destroy(closeTranslator);

                closeText.enableWordWrapping = false;
                closeText.fontSize = 3.5f;
                closeText.text = GetString("CloseLabel"); // oppure: "Esci"

                // Azione pulsante "Chiudi"
                var closeButtonComponent = closeBtn.GetComponent<PassiveButton>();
                closeButtonComponent.OnClick = new();
                closeButtonComponent.OnClick.AddListener((Action)(() =>
                {
                    InfoPopup.gameObject.SetActive(false);
                }));
            }
        }
    }

}
