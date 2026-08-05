pub mod win_input;

use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(tag = "type", content = "payload")]
pub enum RemoteInputEvent {
    MouseMove { x: f32, y: f32 }, // Normalized [0.0, 1.0]
    MouseDown { button: u8 },      // 0: Left, 1: Middle, 2: Right
    MouseUp { button: u8 },
    MouseWheel { delta_y: i32 },
    KeyDown { vk_code: u16, key: String },
    KeyUp { vk_code: u16, key: String },
}

pub trait InputInjector {
    fn inject_event(&mut self, event: RemoteInputEvent) -> Result<(), String>;
}
