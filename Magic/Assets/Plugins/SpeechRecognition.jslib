// SpeechRecognition.jslib - STT + TTS 통합 버전 (정리된 버전)
mergeInto(LibraryManager.library, {
    // === STT 관련 함수들 ===
    InitializeSpeechRecognition: function() {
        if (!window.speechRecognitionData) {
            window.speechRecognitionData = {
                recognition: null,
                isListening: false
            };
        }
        
        if (!('webkitSpeechRecognition' in window) && !('SpeechRecognition' in window)) {
            return 0;
        }
        
        var SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        window.speechRecognitionData.recognition = new SpeechRecognition();
        
        window.speechRecognitionData.recognition.continuous = false;
        window.speechRecognitionData.recognition.interimResults = false;
        window.speechRecognitionData.recognition.lang = 'en-US';
        
        window.speechRecognitionData.recognition.onresult = function(event) {
            var result = event.results[0][0].transcript;
            var confidence = event.results[0][0].confidence;
            SendMessage('VoiceRecognitionManager', 'OnWebSpeechResult', result + "|" + confidence);
        };
        
        window.speechRecognitionData.recognition.onerror = function(event) {
            SendMessage('VoiceRecognitionManager', 'OnWebSpeechError', event.error);
        };
        
        window.speechRecognitionData.recognition.onend = function() {
            window.speechRecognitionData.isListening = false;
            SendMessage('VoiceRecognitionManager', 'OnWebSpeechEnd', '');
        };
        
        return 1;
    },
    
    StartSpeechRecognition: function() {
        if (window.speechRecognitionData && window.speechRecognitionData.recognition && !window.speechRecognitionData.isListening) {
            try {
                window.speechRecognitionData.recognition.start();
                window.speechRecognitionData.isListening = true;
                return 1;
            } catch (e) {
                return 0;
            }
        }
        return 0;
    },
    
    StopSpeechRecognition: function() {
        if (window.speechRecognitionData && window.speechRecognitionData.recognition && window.speechRecognitionData.isListening) {
            window.speechRecognitionData.recognition.stop();
            window.speechRecognitionData.isListening = false;
            return 1;
        }
        return 0;
    },
    
    IsListening: function() {
        if (window.speechRecognitionData) {
            return window.speechRecognitionData.isListening ? 1 : 0;
        }
        return 0;
    },
    
    // === TTS 관련 함수들 ===
    InitializeTTS: function() {
        if (!window.ttsData) {
            window.ttsData = {
                isSpeaking: false,
                currentUtterance: null,
                voices: []
            };
        }
        
        if (!('speechSynthesis' in window)) {
            return 0;
        }
        
        function loadVoices() {
            window.ttsData.voices = speechSynthesis.getVoices();
        }
        
        speechSynthesis.addEventListener('voiceschanged', loadVoices);
        loadVoices();
        
        return 1;
    },
    
    SpeakTextJS: function(textPtr, rate, pitch, voicePtr) {
        if (!('speechSynthesis' in window)) {
            return 0;
        }
        
        var text = UTF8ToString(textPtr);
        var voiceLang = UTF8ToString(voicePtr);
        
        if (!text || text.trim() === '') {
            return 0;
        }
        
        // 현재 재생 중인 음성 중지
        if (window.ttsData && window.ttsData.isSpeaking) {
            speechSynthesis.cancel();
            window.ttsData.isSpeaking = false;
            window.ttsData.currentUtterance = null;
        }
        
        var utterance = new SpeechSynthesisUtterance(text);
        utterance.rate = Math.max(0.1, Math.min(10, rate));
        utterance.pitch = Math.max(0, Math.min(2, pitch));
        utterance.lang = voiceLang;
        
        // 선호하는 음성 찾기
        if (window.ttsData.voices.length > 0) {
            var preferredVoice = null;
            
            // 정확한 음성 이름으로 검색
            preferredVoice = window.ttsData.voices.find(function(voice) {
                return voice.name === voiceLang;
            });
            
            // 정확한 이름이 없으면 언어 코드로 검색
            if (!preferredVoice) {
                preferredVoice = window.ttsData.voices.find(function(voice) {
                    return voice.lang === voiceLang || voice.lang.startsWith(voiceLang.split('-')[0]);
                });
            }
            
            if (preferredVoice) {
                utterance.voice = preferredVoice;
            }
        }
        
        // 이벤트 리스너 설정
        utterance.onstart = function() {
            if (window.ttsData) {
                window.ttsData.isSpeaking = true;
                window.ttsData.currentUtterance = utterance;
            }
            SendMessage('TextToSpeechManager', 'OnTTSStart', '');
        };
        
        utterance.onend = function() {
            if (window.ttsData) {
                window.ttsData.isSpeaking = false;
                window.ttsData.currentUtterance = null;
            }
            SendMessage('TextToSpeechManager', 'OnTTSEnd', '');
        };
        
        utterance.onerror = function(event) {
            if (window.ttsData) {
                window.ttsData.isSpeaking = false;
                window.ttsData.currentUtterance = null;
            }
            SendMessage('TextToSpeechManager', 'OnTTSError', event.error);
        };
        
        try {
            speechSynthesis.speak(utterance);
            return 1;
        } catch (e) {
            return 0;
        }
    },
    
    StopTTS: function() {
        if ('speechSynthesis' in window) {
            speechSynthesis.cancel();
            if (window.ttsData) {
                window.ttsData.isSpeaking = false;
                window.ttsData.currentUtterance = null;
            }
            return 1;
        }
        return 0;
    },
    
    IsTTSSpeaking: function() {
        if ('speechSynthesis' in window) {
            var speaking = speechSynthesis.speaking;
            if (window.ttsData) {
                window.ttsData.isSpeaking = speaking;
            }
            return speaking ? 1 : 0;
        }
        return 0;
    }
});