using System.Collections.Generic;
using UnityEngine;

namespace DialogueAI
{
    // This stores info for the Cuai TTS API call
    public class Dialogue
    {
        public AudioClip audioClip;
        public AudioClip clip;
        public string requestId { get; set; }
        public string character { get; set; }
        public string text { get; set; }
        public string voiceModel { get; set; }
        public int clipNumber { get; set; }
        public bool failed { get; set; }
    }

    // Response from Cuai TTS API /generate-speech endpoint
    public class CuaiTTSResponse
    {
        public string filename { get; set; }
        public string message { get; set; }
    }

    // Voice info from /voices endpoint
    public class CuaiVoiceInfo
    {
        public string voice_name { get; set; }
        public float duration { get; set; }
        public int sample_rate { get; set; }
    }

    // Voices list response
    public class CuaiVoicesResponse
    {
        public List<CuaiVoiceInfo> voices { get; set; }
    }
}