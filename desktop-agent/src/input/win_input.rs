use super::{InputInjector, RemoteInputEvent};

#[cfg(target_os = "windows")]
use windows::Win32::UI::Input::KeyboardAndMouse::*;
#[cfg(target_os = "windows")]
use windows::Win32::UI::WindowsAndMessaging::*;

pub struct Win32InputInjector {
    screen_width: i32,
    screen_height: i32,
}

impl Win32InputInjector {
    pub fn new() -> Self {
        Self {
            screen_width: 1920,
            screen_height: 1080,
        }
    }
}

impl InputInjector for Win32InputInjector {
    fn inject_event(&mut self, event: RemoteInputEvent) -> Result<(), String> {
        match event {
            RemoteInputEvent::MouseMove { x, y } => {
                let abs_x = (x * 65535.0) as i32;
                let abs_y = (y * 65535.0) as i32;

                #[cfg(target_os = "windows")]
                unsafe {
                    let mut input = INPUT::default();
                    input.r#type = INPUT_MOUSE;
                    input.Anonymous.mi.dx = abs_x;
                    input.Anonymous.mi.dy = abs_y;
                    input.Anonymous.mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE;
                    SendInput(&[input], std::mem::size_of::<INPUT>() as i32);
                }
                println!("[Input] Mouse Move: ({:.2}, {:.2}) -> ABS({}, {})", x, y, abs_x, abs_y);
            }

            RemoteInputEvent::MouseDown { button } => {
                #[cfg(target_os = "windows")]
                unsafe {
                    let mut input = INPUT::default();
                    input.r#type = INPUT_MOUSE;
                    input.Anonymous.mi.dwFlags = match button {
                        0 => MOUSEEVENTF_LEFTDOWN,
                        1 => MOUSEEVENTF_MIDDLEDOWN,
                        2 => MOUSEEVENTF_RIGHTDOWN,
                        _ => MOUSEEVENTF_LEFTDOWN,
                    };
                    SendInput(&[input], std::mem::size_of::<INPUT>() as i32);
                }
                println!("[Input] Mouse Down: Button {}", button);
            }

            RemoteInputEvent::MouseUp { button } => {
                #[cfg(target_os = "windows")]
                unsafe {
                    let mut input = INPUT::default();
                    input.r#type = INPUT_MOUSE;
                    input.Anonymous.mi.dwFlags = match button {
                        0 => MOUSEEVENTF_LEFTUP,
                        1 => MOUSEEVENTF_MIDDLEUP,
                        2 => MOUSEEVENTF_RIGHTUP,
                        _ => MOUSEEVENTF_LEFTUP,
                    };
                    SendInput(&[input], std::mem::size_of::<INPUT>() as i32);
                }
                println!("[Input] Mouse Up: Button {}", button);
            }

            RemoteInputEvent::MouseWheel { delta_y } => {
                #[cfg(target_os = "windows")]
                unsafe {
                    let mut input = INPUT::default();
                    input.r#type = INPUT_MOUSE;
                    input.Anonymous.mi.dwFlags = MOUSEEVENTF_WHEEL;
                    input.Anonymous.mi.mouseData = (delta_y * 120) as u32;
                    SendInput(&[input], std::mem::size_of::<INPUT>() as i32);
                }
                println!("[Input] Mouse Wheel Delta: {}", delta_y);
            }

            RemoteInputEvent::KeyDown { vk_code, key } => {
                #[cfg(target_os = "windows")]
                unsafe {
                    let mut input = INPUT::default();
                    input.r#type = INPUT_KEYBOARD;
                    input.Anonymous.ki.wVk = VIRTUAL_KEY(vk_code);
                    SendInput(&[input], std::mem::size_of::<INPUT>() as i32);
                }
                println!("[Input] Key Down: VK {} ({})", vk_code, key);
            }

            RemoteInputEvent::KeyUp { vk_code, key } => {
                #[cfg(target_os = "windows")]
                unsafe {
                    let mut input = INPUT::default();
                    input.r#type = INPUT_KEYBOARD;
                    input.Anonymous.ki.wVk = VIRTUAL_KEY(vk_code);
                    input.Anonymous.ki.dwFlags = KEYEVENTF_KEYUP;
                    SendInput(&[input], std::mem::size_of::<INPUT>() as i32);
                }
                println!("[Input] Key Up: VK {} ({})", vk_code, key);
            }
        }
        Ok(())
    }
}
