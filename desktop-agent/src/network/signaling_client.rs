use futures_util::{SinkExt, StreamExt};
use serde::{Deserialize, Serialize};
use tokio_tungstenite::{connect_async, tungstenite::protocol::Message};
use std::sync::Arc;
use tokio::sync::Mutex;

#[derive(Debug, Serialize, Deserialize)]
pub struct SignalingFrame {
    pub r#type: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub targetId: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub senderId: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub payload: Option<serde_json::Value>,
}

pub struct SignalingClient {
    server_url: String,
    pub assigned_host_id: Arc<Mutex<Option<String>>>,
}

impl SignalingClient {
    pub fn new(server_url: &str) -> Self {
        Self {
            server_url: server_url.to_string(),
            assigned_host_id: Arc::new(Mutex::new(None)),
        }
    }

    pub async fn connect_and_register(&self) -> Result<(), Box<dyn std::error::Error>> {
        let (ws_stream, _) = connect_async(&self.server_url).await?;
        println!("[Signaling Client] Connected to signaling server: {}", self.server_url);

        let (mut write, mut read) = ws_stream.split();

        // 1. Send REGISTER_HOST
        let reg_msg = SignalingFrame {
            r#type: "REGISTER_HOST".to_string(),
            targetId: None,
            senderId: None,
            payload: None,
        };
        let msg_str = serde_json::to_string(&reg_msg)?;
        write.send(Message::Text(msg_str)).await?;

        let host_id_arc = self.assigned_host_id.clone();

        // 2. Incoming message loop
        tokio::spawn(async move {
            while let Some(msg) = read.next().await {
                match msg {
                    Ok(Message::Text(text)) => {
                        if let Ok(frame) = serde_json::from_str::<SignalingFrame>(&text) {
                            match frame.r#type.as_str() {
                                "HOST_REGISTERED" => {
                                    if let Some(payload) = frame.payload {
                                        if let Some(host_id) = payload.get("hostId").and_then(|v| v.as_str()) {
                                            let formatted = payload.get("formattedId").and_then(|v| v.as_str()).unwrap_or(host_id);
                                            println!("=======================================================");
                                            println!("  🎮 AETHERDESK HOST AGENT ONLINE");
                                            println!("  YOUR 9-DIGIT SESSION ID: [ {} ]", formatted);
                                            println!("=======================================================");
                                            let mut guard = host_id_arc.lock().await;
                                            *guard = Some(host_id.to_string());
                                        }
                                    }
                                }
                                "CONNECT_REQUEST" => {
                                    println!("[Signaling] Connection request received from peer: {:?}", frame.senderId);
                                    // Handle SDP exchange / Auth validation
                                }
                                "SDP_OFFER" => {
                                    println!("[Signaling] Received SDP Offer from peer: {:?}", frame.senderId);
                                }
                                "ICE_CANDIDATE" => {
                                    println!("[Signaling] Received ICE Candidate from peer: {:?}", frame.senderId);
                                }
                                "PONG" => {}
                                _ => println!("[Signaling] Unhandled message type: {}", frame.r#type),
                            }
                        }
                    }
                    Ok(Message::Ping(_)) => {
                        let _ = write.send(Message::Pong(vec![])).await;
                    }
                    Err(e) => {
                        println!("[Signaling Client Error] {}", e);
                        break;
                    }
                    _ => {}
                }
            }
        });

        Ok(())
    }
}
