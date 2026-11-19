// src/components/Dashboar/agregarServicios.jsx
import React, { useState } from "react";
import { uploadImageToGitHub } from "../../utils/githubUpload"; // 👈 IMPORTANTE: ajusta la ruta
import "./agregarServicios.css";

const AgregarServicios = ({ onClose }) => { // Recibe onClose como prop para cerrar modal
  const [preview, setPreview] = useState(null);
  const [selectedFile, setSelectedFile] = useState(null);
  const [uploading, setUploading] = useState(false);
  
  // Estados para los campos del formulario
  const [nombre, setNombre] = useState("");
  const [descripcion, setDescripcion] = useState("");
  const [precio, setPrecio] = useState("");

  const handleDrop = (e) => {
    e.preventDefault();
    const file = e.dataTransfer.files[0];
    if (file && file.type.startsWith("image/")) {
      setSelectedFile(file);
      setPreview(URL.createObjectURL(file));
    }
  };

  const handleDragOver = (e) => {
    e.preventDefault();
  };

  const handleChange = (e) => {
    const file = e.target.files[0];
    if (file) {
      setSelectedFile(file);
      setPreview(URL.createObjectURL(file));
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!selectedFile) {
      alert("Por favor selecciona una imagen");
      return;
    }
    
    if (!nombre) {
      alert("Por favor ingresa el nombre del servicio");
      return;
    }
    
    setUploading(true);
    
    try {
      // Subir imagen a GitHub
      const imageUrl = await uploadImageToGitHub(selectedFile, nombre);
      
      // Aquí puedes guardar los datos del servicio
      // Por ahora solo mostramos éxito
      alert(`✅ Servicio "${nombre}" agregado correctamente\nURL: ${imageUrl}`);
      
      // Limpiar formulario
      setNombre("");
      setDescripcion("");
      setPrecio("");
      setSelectedFile(null);
      setPreview(null);
      
      // Cerrar modal (si tienes la función)
      if (onClose) onClose();
      
    } catch (error) {
      alert("❌ Error: " + error.message);
    } finally {
      setUploading(false);
    }
  };

  return (
    <div>
      <h2>Nuevo Servicio</h2>
      <p>Agrega un nuevo servicio al catálogo con su imagen</p>

      <form className="form-servicios" onSubmit={handleSubmit}>
        <label>Imagen del Servicio*</label>
        <div 
          className="drop-area"
          onDrop={handleDrop}
          onDragOver={handleDragOver}
        >
          <label htmlFor="imagen">
            <p>Arrastra una imagen aquí o haz clic para seleccionar</p>
          </label>
          <input
            id="imagen"
            type="file"
            accept="image/*"
            onChange={handleChange}
          />
        </div>

        {preview && (
          <img
            src={preview}
            alt="preview"
            className="preview-img"
            style={{ maxWidth: '200px', marginTop: '10px' }}
          />
        )}

        <label htmlFor="nombre">Nombre del Servicio:</label>
        <input 
          type="text" 
          id="nombre" 
          name="nombre" 
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          required 
        />

        <label htmlFor="descripcion">Descripción:</label>
        <textarea 
          id="descripcion" 
          name="descripcion"
          value={descripcion}
          onChange={(e) => setDescripcion(e.target.value)}
          required
        />

        <label htmlFor="precio">Precio:</label>
        <input 
          type="number" 
          id="precio" 
          name="precio"
          value={precio}
          onChange={(e) => setPrecio(e.target.value)}
          required 
        />

        <button 
          type="submit" 
          disabled={uploading}
        >
          {uploading ? "Subiendo..." : "Agregar Servicio"}
        </button>
      </form>
    </div>
  );
};

export default AgregarServicios; //ya mero?