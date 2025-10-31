using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.AIControllers
{
    public static class Utils
    {
        // takes the chatgpt output and seperates it into a string array, each string in the array is a new dialog line or an action.
        // this also is where we remove a bunch of nono
        public static string[] ProcessOutputIntoStringArray(string chatgptOutputMessage, ref string message)
        {
            // message = messages[messages.Count - 1].Content;
            message = chatgptOutputMessage;
            Debug.Log(message);
            // textField.text = message;


            char[] delims = new[] { '\r', '\n' };
            string[] outputLinesProcessed = message.Split(delims, StringSplitOptions.RemoveEmptyEntries);
            return outputLinesProcessed;
        }


        public static string[] AddSwearing(string[] chatgptOutputMessageLines)
        {
            string[] chatgptOutputMessageLinesWithSwearing = new string[chatgptOutputMessageLines.Length];
            for (int i = 0; i < chatgptOutputMessageLines.Length; i++)
            {
                chatgptOutputMessageLinesWithSwearing[i]= chatgptOutputMessageLines[i];
                chatgptOutputMessageLinesWithSwearing[i] = chatgptOutputMessageLinesWithSwearing[i].Replace("frick", "fuck");
                chatgptOutputMessageLinesWithSwearing[i] = chatgptOutputMessageLinesWithSwearing[i].Replace("Frick", "Fuck");
                chatgptOutputMessageLinesWithSwearing[i] = chatgptOutputMessageLinesWithSwearing[i].Replace("Freakin", "Fuckin");
                chatgptOutputMessageLinesWithSwearing[i] = chatgptOutputMessageLinesWithSwearing[i].Replace("freakin", "fuckin");
                chatgptOutputMessageLinesWithSwearing[i] = chatgptOutputMessageLinesWithSwearing[i].Replace("crap", "shit");
                chatgptOutputMessageLinesWithSwearing[i] = chatgptOutputMessageLinesWithSwearing[i].Replace("Crap", "Shit");
                chatgptOutputMessageLinesWithSwearing[i] = chatgptOutputMessageLinesWithSwearing[i].Replace("shoot", "shit");
                chatgptOutputMessageLinesWithSwearing[i] = chatgptOutputMessageLinesWithSwearing[i].Replace("Shoot", "Shit");
            }

            return chatgptOutputMessageLinesWithSwearing;
        }
    }
}
