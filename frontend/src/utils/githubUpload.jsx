// src/utils/githubUpload.js

// TUS DATOS - NO CAMBIES ESTO
const GITHUB_OWNER = "Palchueck";
const GITHUB_REPO = "imagenes-dentista";
const GITHUB_BRANCH = "main";

// IMPORTANTE: Vas a pegar tu token aquí TEMPORALMENTE para probar
const GITHUB_TOKEN = ""; // 👈 PEGA TU TOKEN AQUÍ

// Función para convertir archivo a Base64
const fileToBase64 = (file) => {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.readAsDataURL(file);
    reader.onload = () => {
      // Quita el prefijo (data:image/jpeg;base64,)
      const base64 = reader.result.split(',')[1];
      resolve(base64);
    };
    reader.onerror = reject;
  });
};

// Función principal para subir imagen
export const uploadImageToGitHub = async (file, serviceName) => {
  try {
    console.log("📤 Subiendo imagen...");
    
    // 1. Convertir imagen a Base64
    const base64Content = await fileToBase64(file);
    
    // 2. Crear nombre del archivo
    const extension = file.name.split('.').pop();
    const timestamp = Date.now();
    // Tomar primera palabra del servicio
    const serviceWord = serviceName.split(' ')[0];
    const fileName = `imagenes/${serviceWord}Dental-${timestamp}.${extension}`;
    
    // 3. Llamar a la API de GitHub
    const url = `https://api.github.com/repos/${GITHUB_OWNER}/${GITHUB_REPO}/contents/${fileName}`;
    
    const response = await fetch(url, {
      method: 'PUT',
      headers: {
        'Authorization': `token ${GITHUB_TOKEN}`,
        'Accept': 'application/vnd.github.v3+json',
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        message: `Agregar imagen: ${serviceName}`,
        content: base64Content,
        branch: GITHUB_BRANCH,
      })
    });
    
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message);
    }
    
    const result = await response.json();
    
    // 4. Obtener URL pública
    const imageUrl = `https://raw.githubusercontent.com/${GITHUB_OWNER}/${GITHUB_REPO}/${GITHUB_BRANCH}/${fileName}`;
    
    console.log("✅ Imagen subida:", imageUrl);
    return imageUrl;
    
  } catch (error) {
    console.error("❌ Error:", error);
    throw error;
  }
};