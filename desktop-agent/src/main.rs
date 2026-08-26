mod capture;
mod input;
mod network;
mod file_transfer;
mod security;
mod service;

use capture::{
    dxgi_win::DxgiCapturer,
    nvenc_encoder::NvencH264Encoder,
    ScreenCapturer, VideoEncoder
};
use input::{win_input::Win32InputInjector, InputInjector, RemoteInputEvent};
use network::{direct_listener::DirectIPListener, signaling_client::SignalingClient};
use security::{auth::UnattendedAuth, guard::SecurityGuard};
use service::autostart::AutoStartManager;

fn is_admin() -> bool {
    std::process::Command::new("net")
        .arg("session")
        .stdout(std::process::Stdio::null())
        .stderr(std::process::Stdio::null())
        .status()
        .map(|s| s.success())
        .unwrap_or(false)
}

fn handle_self_install() {
    if std::env::args().any(|arg| arg == "--install") {
        println!("[Installer] Self-installation triggered...");
        if !is_admin() {
            println!("[Installer] Requesting Administrator privileges for background service installation...");
            if let Ok(current_exe) = std::env::current_exe() {
                let status = std::process::Command::new("powershell")
                    .args(&[
                        "-Command",
                        &format!("Start-Process '{}' -ArgumentList '--install' -Verb RunAs", current_exe.to_string_lossy())
                    ])
                    .status();
                if status.is_ok() {
                    std::process::exit(0);
                }
            }
            eprintln!("[Installer ERROR] Failed to elevate privileges.");
            std::process::exit(1);
        }

        // We are elevated. Proceed with installation.
        let current_exe = std::env::current_exe().expect("Failed to get current executable path");
        let target_dir = std::path::Path::new("C:\\Program Files\\AetherDesk\\Agent");
        if !target_dir.exists() {
            if let Err(e) = std::fs::create_dir_all(target_dir) {
                eprintln!("[Installer ERROR] Failed to create installation folder: {}", e);
                std::process::exit(1);
            }
        }

        let target_exe = target_dir.join("aetherdesk-agent.exe");
        
        // Prevent copying if we are already running from target path
        if current_exe != target_exe {
            if let Err(e) = std::fs::copy(&current_exe, &target_exe) {
                eprintln!("[Installer ERROR] Failed to copy binary: {}", e);
                std::process::exit(1);
            }
        }

        // Register startup Registry key
        if let Err(e) = service::autostart::AutoStartManager::enable_autostart(&target_exe.to_string_lossy()) {
            eprintln!("[Installer ERROR] Failed to register startup registry key: {}", e);
            std::process::exit(1);
        }

        // Add Windows Defender Firewall rule
        println!("[Installer] Adding Firewall rule...");
        let _ = std::process::Command::new("cmd")
            .args(&[
                "/c",
                "netsh advfirewall firewall add rule name=\"AetherDesk Agent Inbound\" dir=in action=allow protocol=TCP localport=8443 enable=yes"
            ])
            .status();

        println!("=======================================================");
        println!("  ✅ AetherDesk Agent installed successfully!");
        println!("  Installed at: C:\\Program Files\\AetherDesk\\Agent\\aetherdesk-agent.exe");
        println!("=======================================================");
        
        // Launch in background from target path
        let _ = std::process::Command::new("cmd")
            .args(&[
                "/c",
                "start \"\" \"C:\\Program Files\\AetherDesk\\Agent\\aetherdesk-agent.exe\""
            ])
            .status();

        std::process::exit(0);
    }
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    tracing_subscriber::fmt::init();
    
    // Process self-installer if triggered
    handle_self_install();

    println!("=======================================================");
    println!("  ⚡ AetherDesk Native Host Agent Starting...");
    println!("  Hardware Acceleration: DXGI Duplication + NVENC H.264");
    println!("=======================================================");


    // 0. Enable Windows Auto-Start on System Boot
    if let Ok(exe_path) = std::env::current_exe() {
        let _ = AutoStartManager::enable_autostart(&exe_path.to_string_lossy());
    }

    // 0b. Start Background Security Guard & Trojan Monitoring
    let security_guard = SecurityGuard::new();
    security_guard.start_monitoring("dev_01".to_string(), "HQ Server Room 01".to_string()).await;

    // 1. Initialize Unattended Password Security
    let _auth = UnattendedAuth::new("aether2026");


    // 2. Start Direct IP:Port Listener (Port 8443)
    let direct_listener = DirectIPListener::new(8443);
    direct_listener.start_listener().await?;

    // 3. Connect to WebSocket Signaling Server
    let signaling_url = std::env::var("SIGNALING_URL").unwrap_or_else(|_| "ws://localhost:8080".to_string());
    let signaling_client = SignalingClient::new(&signaling_url);
    if let Err(e) = signaling_client.connect_and_register().await {
        println!("[Warning] Could not reach signaling server: {}. Operating in Direct IP mode.", e);
    }

    // 4. Initialize 60 FPS DXGI Capturer & NVENC Hardware Encoder
    let mut capturer = DxgiCapturer::new().expect("Failed to initialize DXGI Screen Capturer");
    let mut encoder = NvencH264Encoder::new(1920, 1080, 60, 4000).expect("Failed to initialize NVENC Encoder");
    let mut injector = Win32InputInjector::new();

    // 5. Sample Capture & Encode Loop
    if let Ok(raw_frame) = capturer.capture_frame() {
        println!(
            "[DXGI 60FPS] Captured Frame: {}x{}, Dirty Rects: {}, Full Refresh: {}",
            raw_frame.width,
            raw_frame.height,
            raw_frame.dirty_region.dirty_rects.len(),
            raw_frame.dirty_region.is_full_frame
        );

        if let Ok(h264_frame) = encoder.encode_frame(&raw_frame) {
            println!(
                "[NVENC H.264] Encoded Frame Size: {} bytes, Keyframe: {}",
                h264_frame.payload.len(),
                h264_frame.is_keyframe
            );
        }
    }

    println!("[AetherDesk Agent] Host Agent active and running. Press Ctrl+C to stop.");
    tokio::signal::ctrl_c().await?;
    println!("[AetherDesk Agent] Shutting down host agent.");

    Ok(())
}
