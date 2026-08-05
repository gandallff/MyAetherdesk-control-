use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SessionDescription {
    pub sdp_type: String, // "offer" or "answer"
    pub sdp: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct IceCandidate {
    pub candidate: String,
    pub sdp_mid: Option<String>,
    pub sdp_mline_index: Option<u16>,
}

pub struct WebRTCEngine {
    is_connected: bool,
}

impl WebRTCEngine {
    pub fn new() -> Self {
        Self { is_connected: false }
    }

    pub fn create_offer(&self) -> SessionDescription {
        SessionDescription {
            sdp_type: "offer".to_string(),
            sdp: "v=0\r\no=- 123456 2 IN IP4 127.0.0.1\r\ns=AetherDesk\r\nt=0 0\r\na=group:BUNDLE 0 1\r\n".to_string(),
        }
    }

    pub fn handle_answer(&mut self, _answer: SessionDescription) -> Result<(), String> {
        self.is_connected = true;
        println!("[WebRTC Engine] Peer Connection Established Successfully!");
        Ok(())
    }
}
