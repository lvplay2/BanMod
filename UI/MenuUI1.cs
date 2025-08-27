using BanMod;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod
{
    public class MenuUI1 : MonoBehaviour
    {
        public static string buttonFilePath = "BAN_DATA/MENU/buttonmessage.txt";

        public static void Initialize()
        {
            try
            {
                Directory.CreateDirectory("BAN_DATA/MENU/");
                if (!File.Exists(buttonFilePath))
                {
                    File.Create(buttonFilePath).Close();
                }
            }
            catch (Exception) { }
        }

        private bool showMenu = false;
        private Rect windowRect;
        private Vector2 windowSize = new Vector2(560, 620);

        private GUIStyle titleStyle;
        private GUIStyle exitButtonStyle;

        private List<ButtonData> buttonDataList = new List<ButtonData>();
        private KeyCode menuKeybind1 = KeyCode.PageUp;
        private int columns = 3;
        private int rows = 5;
        void Start()
        {
            LoadButtonData();
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
                if (Input.GetKeyDown(menuKeybind1))
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
            GUILayout.Label(GetString("MENU_MSG"), titleStyle, GUILayout.Height(50));
            GUILayout.Space(10);

            ShowButtonContent();

            GUILayout.FlexibleSpace();
            GUILayout.Space(-70);

            if (GUILayout.Button(GetString("EXIT"), exitButtonStyle, GUILayout.Width(windowSize.x)))
            {
                showMenu = false;
            }

            GUI.DragWindow();
        }

        void ShowButtonContent()
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
                            Utils.SendMessage("", 255, buttonData.Message.Replace("\\n", "\n"));
                            MessageBlocker.UpdateLastMessageTime();
                        }
                        current++;
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
        }

        void LoadButtonData()
        {
            if (!File.Exists(buttonFilePath))
            {
                CreateButtonExampleFile();
            }

            string[] lines = File.ReadAllLines(buttonFilePath);
            buttonDataList.Clear();

            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length == 2)
                {
                    string title = parts[0].Trim();
                    string message = parts[1].Trim();
                    buttonDataList.Add(new ButtonData { Title = title, Message = message });
                }
            }
        }

        void CreateButtonExampleFile()
        {
            string[] exampleLines = new string[]
            {
                "Button 1|Message for Button 1",
                "Button 2|Message for Button 2",
                "Button 3|Message for Button 3"
            };
            File.WriteAllLines(buttonFilePath, exampleLines);
        }

        public class ButtonData
        {
            public string Title { get; set; }
            public string Message { get; set; }
        }
    }
}

public class MenuController1 : MonoBehaviour
{
    public MenuUI1 menuUI1;  // Riferimento al tuo primo menu

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3))  // Tasto 3
        {
            if (menuUI1 != null)
            {
                if (menuUI1.IsOpen())
                    menuUI1.CloseMenu();
                else
                    menuUI1.OpenMenu();
            }
        }
    }
}