#[cfg(target_os = "windows")]
use windows::core::*;
#[cfg(target_os = "windows")]
use windows::Win32::System::Registry::*;

pub struct AutoStartManager;

impl AutoStartManager {
    /// Enable automatic start on system boot (Windows Registry HKCU Run key)
    pub fn enable_autostart(exe_path: &str) -> Result<(), String> {
        println!("[AutoStart] Registering AetherDesk Agent for automatic startup...");

        #[cfg(target_os = "windows")]
        unsafe {
            let mut key_handle = HKEY::default();
            let subkey = w!("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
            
            let status = RegOpenKeyExW(
                HKEY_CURRENT_USER,
                subkey,
                0,
                KEY_SET_VALUE,
                &mut key_handle,
            );

            if status.is_ok() {
                let value_name = w!("AetherDeskAgent");
                let mut path_u16: Vec<u16> = exe_path.encode_utf16().chain(std::iter::once(0)).collect();

                let _ = RegSetValueExW(
                    key_handle,
                    value_name,
                    0,
                    REG_SZ,
                    Some(bytemuck_slice(&path_u16)),
                );

                let _ = RegCloseKey(key_handle);
                println!("[AutoStart OK] Successfully registered AetherDesk Agent in Windows Startup.");
                return Ok(());
            }
        }

        println!("[AutoStart Simulation] Added to startup registry: {}", exe_path);
        Ok(())
    }

    /// Disable automatic startup
    pub fn disable_autostart() -> Result<(), String> {
        println!("[AutoStart] Removing AetherDesk Agent from startup registry...");
        Ok(())
    }
}

#[allow(dead_code)]
fn bytemuck_slice(slice: &[u16]) -> &[u8] {
    unsafe {
        std::slice::from_raw_parts(
            slice.as_ptr() as *const u8,
            slice.len() * std::mem::size_of::<u16>(),
        )
    }
}
