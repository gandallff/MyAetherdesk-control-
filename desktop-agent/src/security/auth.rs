use sha2::{Digest, Sha256};

pub struct UnattendedAuth {
    password_hash: String,
}

impl UnattendedAuth {
    pub fn new(password: &str) -> Self {
        let hash = Self::hash_password(password);
        Self { password_hash: hash }
    }

    pub fn verify(&self, input_password: &str) -> bool {
        let input_hash = Self::hash_password(input_password);
        self.password_hash == input_hash
    }

    fn hash_password(password: &str) -> String {
        let mut hasher = Sha256::new();
        hasher.update(b"AETHERDESK_SALT_2026_");
        hasher.update(password.as_bytes());
        hex::encode(hasher.finalize())
    }
}
