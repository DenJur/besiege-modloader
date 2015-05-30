﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if DEV_BUILD
using System.IO;
#endif

namespace spaar
{
    public class Console : MonoBehaviour
    {
#if DEV_BUILD
        // In a developer build, all console messages are also written to Mods/Debug/ConsoleOutput.txt to assist in debuggin.
        // This is especially useful because the format in output_log.txt is less than ideal for this use-case.
        private TextWriter tw;
#endif

        private List<string> logMessages;
        private readonly int maxLogMessages = 200;

        private Rect windowRect;
        private Vector2 scrollPosition;
        private string commandText = "";
        private string lastCommand = "";

        private char[] newLine = { '\n', '\r' };

        private bool visible = false;
        private bool interfaceEnabled;
        private Dictionary<LogType, bool> messageFilter;

        public Console()
        {
            interfaceEnabled = false;
        }

        void OnEnable()
        {
            Application.RegisterLogCallback(HandleLog);
            logMessages = new List<string>(maxLogMessages);
            windowRect = new Rect(50f, 50f, 600f, 600f);

            initMessageFiltering();
        }

        private void initMessageFiltering()
        {
            messageFilter = new Dictionary<LogType, bool>();
            messageFilter.Add(LogType.Assert, true);
            messageFilter.Add(LogType.Error, true);
            messageFilter.Add(LogType.Exception, true);
            messageFilter.Add(LogType.Log, true);
            messageFilter.Add(LogType.Warning, true);

            Commands.RegisterCommand("setMessageFilter", (string[] args, IDictionary<string, string> namedArgs) =>
            {
                foreach (var arg in args)
                {
                    bool val = !arg.StartsWith("!");
                    string key = arg;
                    if (!val) key = arg.Substring(1);
                    try
                    {
                        var type = (LogType)Enum.Parse(typeof(LogType), key);
                        Debug.Log("Setting " + type + " to " + val);
                        messageFilter[type] = val;
                    }
                    catch (ArgumentException)
                    {
                        Debug.LogError("Not a valid filter setting: " + arg);
                    }
                }
                return "Successfully updated console message filter.";
            }, "Update the filter settings for console messages. Every argument must be in the form 'type' or '!type'. " + 
               "The first form will activate the specified type. The second one will deactive it. " +
               "Vaild values for type are Assert, Error, Exception, Log and Warning.");
        }

        

        void OnDisable()
        {
            Application.RegisterLogCallback(null);
#if DEV_BUILD
            if (tw != null)
                tw.Close();
#endif
        }

        /// <summary>
        /// Enables the interface which is disabled by default after creating the Console.
        /// </summary>
        public void EnableInterface()
        {
            interfaceEnabled = true;
        }

        void Update()
        {
            if (interfaceEnabled && Input.GetKey(Keys.getKey("Console").Modifier) && Input.GetKeyDown(Keys.getKey("Console").Trigger))
            {
                visible = !visible;
            }
        }

        void OnGUI()
        {
            if (visible)
            {
                windowRect = GUI.Window(-1001, windowRect, OnWindow, "Console");
            }
        }

        void OnWindow(int windowId)
        {
            float lineHeight = GUI.skin.box.lineHeight;

            GUILayout.BeginArea(new Rect(5f, lineHeight + 5f, windowRect.width - 10f, windowRect.height - 30f));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            string logText = "";
            foreach (var s in logMessages)
            {
                logText += s + "\n";
            }
            GUILayout.TextArea(logText);
            GUILayout.EndScrollView();

            bool moveCursor = false;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.UpArrow)
            {
                commandText = lastCommand;
                moveCursor = true;
            }

            string input = GUILayout.TextField(commandText, 100, GUI.skin.textField);

            if (moveCursor)
            {
                TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                editor.pos = commandText.Length + 1;
                editor.selectPos = commandText.Length + 1;
            }
            if (input.IndexOfAny(newLine) != -1)
            {
                commandText = "";
                lastCommand = input.Replace("\n", "").Replace("\r", "");
                Commands.HandleCommand(this, input.Replace("\n", "").Replace("\r", ""));
            }
            else
            {
                commandText = input;
            }

            GUILayout.EndArea();


            GUI.DragWindow();
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            //TODO: still write filtered messages to console output file, if DEV_BUILD
            if (!messageFilter[type])
                return;

            var typeString = "[";
            switch (type)
            {
                case LogType.Assert:
                    typeString += "Assert";
                    break;
                case LogType.Error:
                    typeString += "Error";
                    break;
                case LogType.Exception:
                    typeString += "Exception";
                    break;
                case LogType.Log:
                    typeString += "Log";
                    break;
                case LogType.Warning:
                    typeString += "Warning";
                    break;
            }
            typeString += "] ";

            var logMessage = "";
            if (type == LogType.Exception)
            {
                logMessage = typeString + logString + "\n" + stackTrace;
            }
            else
            {
                logMessage = typeString + logString;
            }

            AddLogMessage(logMessage);
        }

        internal void AddLogMessage(string logMessage)
        {
            if (logMessages.Count < maxLogMessages)
            {
                logMessages.Add(logMessage);
            }
            else
            {
                logMessages.RemoveAt(0);
                logMessages.Add(logMessage);
            }

#if DEV_BUILD
            if (tw == null)
                tw = new StreamWriter(Application.dataPath + "/Mods/Debug/ConsoleOutput.txt");
            var lines = logMessage.Split('\n');
            foreach (var line in lines)
            {
                tw.WriteLine(line);
            }
#endif

            scrollPosition.y = Mathf.Infinity;
        }

        
    }
}
