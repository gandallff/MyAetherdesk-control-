use tokio::net::TcpListener;
use tokio::io::{AsyncReadExt, AsyncWriteExt};

pub struct DirectIPListener {
    bind_port: u16,
}

impl DirectIPListener {
    pub fn new(bind_port: u16) -> Self {
        Self { bind_port }
    }

    pub async fn start_listener(&self) -> Result<(), Box<dyn std::error::Error>> {
        let addr = format!("0.0.0.0:{}", self.bind_port);
        let listener = TcpListener::bind(&addr).await?;
        println!("[Direct IP Listener] Listening for direct connections on {}", addr);

        tokio::spawn(async move {
            loop {
                match listener.accept().await {
                    Ok((mut socket, peer_addr)) => {
                        println!("[Direct IP Connection] New incoming direct P2P link from {}", peer_addr);
                        
                        tokio::spawn(async move {
                            let mut buf = [0u8; 1024];
                            loop {
                                match socket.read(&mut buf).await {
                                    Ok(0) => break, // Connection closed
                                    Ok(n) => {
                                        println!("[Direct IP Raw Bytes] Received {} bytes from {}", n, peer_addr);
                                        // Process raw DTLS / framing or authentication handshake
                                        let response = b"AETHERDESK_DIRECT_ACK\n";
                                        let _ = socket.write_all(response).await;
                                    }
                                    Err(_) => break,
                                }
                            }
                        });
                    }
                    Err(e) => {
                        println!("[Direct IP Accept Error] {}", e);
                    }
                }
            }
        });

        Ok(())
    }
}
