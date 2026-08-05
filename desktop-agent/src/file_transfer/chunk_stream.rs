use sha2::{Digest, Sha256};
use std::collections::HashMap;

pub const CHUNK_SIZE: usize = 64 * 1024; // 64 KB Binary Chunk
pub const HEADER_SIZE: usize = 16;

pub struct IncomingFileTransfer {
    pub file_id: u32,
    pub filename: String,
    pub total_size: u64,
    pub total_chunks: u32,
    pub received_chunks: HashMap<u32, Vec<u8>>,
    pub hasher: Sha256,
}

impl IncomingFileTransfer {
    pub fn new(file_id: u32, filename: String, total_size: u64, total_chunks: u32) -> Self {
        Self {
            file_id,
            filename,
            total_size,
            total_chunks,
            received_chunks: HashMap::new(),
            hasher: Sha256::new(),
        }
    }

    pub fn process_chunk(&mut self, chunk_index: u32, payload: &[u8]) -> Result<bool, String> {
        if self.received_chunks.contains_key(&chunk_index) {
            return Ok(self.is_complete());
        }

        self.hasher.update(payload);
        self.received_chunks.insert(chunk_index, payload.to_vec());

        println!(
            "[File Transfer] File '{}' Received Chunk {} / {} ({:.1}%)",
            self.filename,
            chunk_index + 1,
            self.total_chunks,
            ((chunk_index + 1) as f32 / self.total_chunks as f32) * 100.0
        );

        Ok(self.is_complete())
    }

    pub fn is_complete(&self) -> bool {
        self.received_chunks.len() as u32 == self.total_chunks
    }

    pub fn finalize_checksum(self) -> String {
        let result = self.hasher.finalize();
        hex::encode(result)
    }
}

pub struct FileChunker;

impl FileChunker {
    pub fn create_chunk_packet(file_id: u32, chunk_index: u32, total_chunks: u32, data: &[u8]) -> Vec<u8> {
        let mut packet = Vec::with_capacity(HEADER_SIZE + data.len());
        
        // 1. PacketType (0x0001 = FILE_DATA)
        packet.extend_from_slice(&1u16.to_be_bytes());
        // 2. FileID (4 bytes)
        packet.extend_from_slice(&file_id.to_be_bytes());
        // 3. ChunkIndex (4 bytes)
        packet.extend_from_slice(&chunk_index.to_be_bytes());
        // 4. TotalChunks (4 bytes)
        packet.extend_from_slice(&total_chunks.to_be_bytes());
        // 5. PayloadLen (2 bytes)
        packet.extend_from_slice(&(data.len() as u16).to_be_bytes());
        // 6. Payload
        packet.extend_from_slice(data);

        packet
    }
}
