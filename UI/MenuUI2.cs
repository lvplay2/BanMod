using BanMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static BanMod.Translator;

namespace BanMod
{
    public class MenuUI2 : MonoBehaviour
    {
        public static string commandFilePath = "BAN_DATA/MENU/commandmessage.txt";

        public static void Initialize()
        {
            try
            {
                Directory.CreateDirectory("BAN_DATA/MENU/");

                if (!File.Exists(commandFilePath))
                {
                    File.Create(commandFilePath).Close();
                }
            }
            catch (Exception) { }
        }

        private bool showMenu = false;
        private Rect windowRect;
        private Vector2 windowSize = new Vector2(560, 620);

        private GUIStyle titleStyle;
        private GUIStyle exitButtonStyle;
        private KeyCode menuKeybind2 = KeyCode.PageDown;
        private int columns = 3;
        private int rows = 5;

        private List<ButtonData> buttonDataList = new List<ButtonData>();

        void Start()
        {
            LoadCommandData();
        }

        public void OpenMenu()
        {
            if (!showMenu)
            {
                ToggleMenu();
            }
        }

        public void CloseMenu()
        {
            if (showMenu)
            {
                ToggleMenu();
            }
        }
        public bool IsOpen()
        {
            return showMenu;
        }
        void Update()
        {
            bool buttonsAreVisible = Options.buttonvisibile.GetBool();

            if (!buttonsAreVisible)
            {
                if (Input.GetKeyDown(menuKeybind2))
                {
                    ToggleMenu();
                }
            }
        }
        void ToggleMenu()
        {
            showMenu = !showMenu;
            if (showMenu)
            {
                // Calcola la posizione centrale dello schermo
                float centerX = Screen.width / 2 - windowSize.x / 2;
                float centerY = Screen.height / 2 - windowSize.y / 2;
                windowRect = new Rect(centerX, centerY, windowSize.x, windowSize.y);
            }
        }

        void EnsureStyles()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 26,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter
                };
            }

            if (exitButtonStyle == null)
            {
                exitButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    fixedHeight = 50
                };
                exitButtonStyle.normal.textColor = Color.red;
                exitButtonStyle.hover.textColor = Color.red;
            }
        }

        void OnGUI()
        {
            if (!showMenu) return;

            EnsureStyles();

            GUI.color = Color.white;
            Color windowBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            GUI.backgroundColor = windowBackgroundColor;
            GUI.Box(new Rect(windowRect.x, windowRect.y, windowRect.width, windowRect.height), GUIContent.none);

            windowRect = GUI.Window(0, windowRect, (GUI.WindowFunction)WindowFunction, "");
        }

        void WindowFunction(int id)
        {
            GUILayout.Label(GetString("MENU_CMD"), titleStyle, GUILayout.Height(50));
            GUILayout.Space(10);

            ShowCommandButtons();

            GUILayout.FlexibleSpace();
            GUILayout.Space(-70);

            if (GUILayout.Button(GetString("EXIT"), exitButtonStyle, GUILayout.Width(windowSize.x)))
            {
                showMenu = false;
            }

            GUI.DragWindow();
        }

        void ShowCommandButtons()
        {
            float buttonWidth = windowSize.x / columns;
            float buttonHeight = (windowSize.y - 150) / rows;

            int total = buttonDataList.Count;
            int current = 0;

            while (current < total)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < columns; col++)
                {
                    if (current < total)
                    {
                        var buttonData = buttonDataList[current];
                        if (GUILayout.Button(buttonData.Title, GUILayout.Width(buttonWidth - 10), GUILayout.Height(buttonHeight - 15)))
                        {
                            // --- INIZIO MODIFICA QUI ---
                            string fullCommandMessage = buttonData.Message.Trim(); // Ottieni l'intera stringa del messaggio
                            string command;
                            string subArg; // Contiene tutti gli argomenti rimanenti come una singola stringa

                            // Trova il primo spazio per separare il comando principale dagli argomenti
                            int firstSpaceIndex = fullCommandMessage.IndexOf(' ');

                            if (firstSpaceIndex != -1)
                            {
                                // Se c'è uno spazio, la prima parte è il comando, il resto sono gli argomenti
                                command = fullCommandMessage.Substring(0, firstSpaceIndex);
                                subArg = fullCommandMessage.Substring(firstSpaceIndex + 1);
                            }
                            else
                            {
                                // Se non ci sono spazi, l'intera stringa è il comando e non ci sono argomenti
                                command = fullCommandMessage;
                                subArg = "";
                            }

                            // Ricostruiamo 'parts' per adattarci alla firma originale di HandleCommand
                            // Questo array conterrà il comando principale come primo elemento,
                            // seguito da ogni parola degli argomenti come elementi separati.
                            List<string> tempParts = new List<string> { command };
                            if (!string.IsNullOrEmpty(subArg))
                            {
                                tempParts.AddRange(subArg.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                            }

                            Debug.Log($"Comando inviato da MenuUI2: {command} Argomenti completi: \"{subArg}\"");

                            // Chiama HandleCommand passando il comando principale, l'array di tutte le parti
                            // e la stringa completa degli argomenti.
                            ChatCommands.HandleCommand(command, tempParts.ToArray(), subArg);
                            // --- FINE MODIFICA QUI ---
                        }
                        current++;
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
        }

        void LoadCommandData()
        {
            if (!File.Exists(commandFilePath))
            {
                CreateCommandExampleFile();
            }

            string[] lines = File.ReadAllLines(commandFilePath);
            buttonDataList.Clear();

            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length == 2)
                {
                    string title = parts[0].Trim();
                    string command = parts[1].Trim();
                    buttonDataList.Add(new ButtonData { Title = title, Message = command });
                }
            }
        }

        void CreateCommandExampleFile()
        {
            string[] exampleLines = new string[]
            {
                "HELP|/help",
                "ID|/id",
                "END MEETING|/end",
                "SEND INFO|/info",
                "PURPLE NAME|/n purple",
                "YELLOW NAME|/n yellow",
                "RED NAME|/n red",
                "RESET MY NAME|/reset",
                "STAR to name|/s star yellow", // Modificato per includere l'esempio a tre parole
                "RENAME PLAYERS|/mnall",
            };
            File.WriteAllLines(commandFilePath, exampleLines);
        }

        public class ButtonData
        {
            public string Title { get; set; }
            public string Message { get; set; }
        }
    }
}

// Questa classe MenuController2 sembra non essere più necessaria se MenuUI2 è gestito da BanMenuButtonsPatch.
// Se la stai usando per un'altra scorciatoia da tastiera (Tasto 3), dovrebbe essere MenuUI2 e non MenuUI1 come riferimento.
// Se il tuo intento è aprire MenuUI2 tramite Tasto 3, la variabile dovrebbe essere 'public MenuUI2 menuUI2;' e non 'MenuUI1'.
// Se è un errore, puoi ignorare questo blocco. Se la stai usando, controlla il riferimento.
public class MenuController2 : MonoBehaviour
{
    public MenuUI2 menuUI2; // <--- Ho cambiato il tipo da MenuUI1 a MenuUI2 qui

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3)) // Tasto 3
        {
            if (menuUI2 != null)
            {
                if (menuUI2.IsOpen())
                    menuUI2.CloseMenu();
                else
                    menuUI2.OpenMenu();
            }
        }
    }
}