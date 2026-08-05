pub mod dxgi_win;
pub mod nvenc_encoder;

#[derive(Debug, Clone, Copy)]
pub struct Rect {
    pub left: i32,
    pub top: i32,
    pub right: i32,
    pub bottom: i32,
}

#[derive(Debug, Clone)]
pub struct DirtyRegion {
    pub dirty_rects: Vec<Rect>,
    pub is_full_frame: bool,
}

pub struct FrameData {
    pub width: u32,
    pub height: u32,
    pub stride: u32,
    pub data: Vec<u8>,
    pub timestamp_us: u64,
    pub dirty_region: DirtyRegion,
}

pub struct EncodedH264Frame {
    pub payload: Vec<u8>,
    pub is_keyframe: bool,
    pub timestamp_us: u64,
}

pub trait ScreenCapturer {
    fn capture_frame(&mut self) -> Result<FrameData, String>;
}

pub trait VideoEncoder {
    fn encode_frame(&mut self, frame: &FrameData) -> Result<EncodedH264Frame, String>;
}
