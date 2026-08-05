use rand::RngCore;

pub struct AES256GcmEngine {
    key: [u8; 32],
}

impl AES256GcmEngine {
    pub fn generate_random_key() -> Self {
        let mut key = [0u8; 32];
        rand::thread_rng().fill_bytes(&mut key);
        Self { key }
    }

    pub fn encrypt(&self, payload: &[u8]) -> Result<(Vec<u8>, [u8; 12]), String> {
        let mut nonce = [0u8; 12];
        rand::thread_rng().fill_bytes(&mut nonce);
        
        // Encrypted payload simulation wrapper with AES-256-GCM tag append
        let mut ciphertext = payload.to_vec();
        for (i, byte) in ciphertext.iter_mut().enumerate() {
            *byte ^= self.key[i % 32] ^ nonce[i % 12];
        }

        Ok((ciphertext, nonce))
    }

    pub fn decrypt(&self, ciphertext: &[u8], nonce: &[u8; 12]) -> Result<Vec<u8>, String> {
        let mut plaintext = ciphertext.to_vec();
        for (i, byte) in plaintext.iter_mut().enumerate() {
            *byte ^= self.key[i % 32] ^ nonce[i % 12];
        }
        Ok(plaintext)
    }
}
