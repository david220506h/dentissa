import React, { useRef, useEffect, useState } from "react";
import {
  FaTools,
  FaHome,
  FaWrench,
  FaWater,
  FaToilet,
  FaToolbox,
} from "react-icons/fa";
import "./nuestrosServicios.css";

const useInView = (ref, options = {}) => {
  const [isInView, setIsInView] = useState(false);

  useEffect(() => {
    const currentRef = ref.current;
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (options.once && isInView) return;
        setIsInView(entry.isIntersecting);
      },
      {
        threshold: options.threshold || 0.1,
        rootMargin: options.margin || "0px",
      }
    );
    if (currentRef) observer.observe(currentRef);
    return () => {
      if (currentRef) observer.unobserve(currentRef);
    };
  }, [ref, options.once, options.margin, options.threshold, isInView]);

  return isInView;
};

const ServiceCard = ({ icon, title, fullDescription, image, delay = 0 }) => {
  const ref = useRef(null);
  const isInView = useInView(ref, { once: true, margin: "-100px 0px", threshold: 0.2 });

  return (
    <div
      className={`service-card ${isInView ? "animate-in" : ""}`}
      ref={ref}
      style={{
        opacity: isInView ? 1 : 0,
        transform: isInView ? "translateY(0)" : "translateY(50px)",
        transition: `opacity 0.6s ease ${delay}s, transform 0.6s ease ${delay}s`,
      }}
    >
      <div className="service-image">
        <img src={image} alt={title} />
        <div className="service-icon">{icon}</div>
      </div>
      <div className="service-content">
        <h3 className="service-title">{title}</h3>
        <p className="service-description">{fullDescription}</p>
        <div className="service-extended">
          <p className="service-full-description">{fullDescription}</p>
          <a href="#contact" className="service-link">Request Information</a>
        </div>
      </div>
    </div>
  );
};

const Servicios = () => {
  const sectionRef = useRef(null);
  const isInView = useInView(sectionRef, { once: true, margin: "-100px 0px" });

  const services = [
    {
      icon: <FaTools />,
      title: "Evaluación y Diagnóstico Dental",
      fullDescription: "Antes de comenzar cualquier tratamiento, realizamos una evaluación completa de tu salud bucal. Analizamos dientes, encías y estructura oral para detectar caries, infecciones o problemas de alineación, y así recomendar el mejor tratamiento para cada paciente."
    },
    {
      icon: <FaWrench />,
      title: "Limpieza Dental Profesional",
      fullDescription: "Ofrecemos una amplia gama de servicios dentales para cuidar tu salud bucal. Desde limpiezas y revisiones regulares hasta tratamientos especializados como ortodoncia, implantes y estética dental. Nuestro equipo de expertos está comprometido a brindarte la mejor atención personalizada para que tengas una sonrisa saludable y radiante."
    },
    {
      icon: <FaWater />,
      title: "Tratamientos de Encías",
      fullDescription: "Realizamos limpiezas dentales profundas para eliminar placa, sarro y manchas en los dientes. Este tratamiento ayuda a prevenir caries, enfermedades de las encías y mantiene tu sonrisa saludable y brillante."
    },
    {
      icon: <FaToilet />,
      title: "Extracciones Dentales",
      fullDescription: "Ofrecemos tratamientos especializados para combatir enfermedades de las encías como gingivitis y periodontitis. Nuestro objetivo es restaurar la salud de tus encías y prevenir la pérdida dental."
    },
    {
      icon: <FaHome />,
      title: "Ortodoncia",
      fullDescription: "Cuando un diente está demasiado dañado o causa problemas de salud bucal, realizamos extracciones dentales seguras y cuidadosas. Nuestro equipo se asegura de que el procedimiento sea lo más cómodo posible para el paciente."
    },
    {
      icon: <FaToolbox />,
      title: "Implantes y Restauraciones",
      fullDescription: "Realizamos implantes dentales, coronas y restauraciones para recuperar dientes perdidos o dañados. Utilizamos materiales de alta calidad para garantizar una sonrisa natural, funcional y duradera."
    }
  ];

  return (
    <section id="services" className="services">
      <div className="container">
        <h2
          className="section-title"
          ref={sectionRef}
          style={{
            opacity: isInView ? 1 : 0,
            transform: isInView ? "translateY(0)" : "translateY(-30px)",
            transition: "opacity 0.6s ease, transform 0.6s ease",
          }}
        >
          Nuestos Servicios
        </h2>
        <p
          className="section-subtitle"
          style={{
            opacity: isInView ? 1 : 0,
            transition: "opacity 0.6s ease 0.2s",
          }}
        >
          Soluciones completas para tu salud dental
        </p>
        <div className="services-container">
          <div className="services-grid">
            {services.map((service, index) => {
              const primeraPalabra = service.title.split(" ")[0];
              
              return (
                <ServiceCard
                  key={index}
                  icon={service.icon}
                  title={service.title}
                  fullDescription={service.fullDescription}
                  image={`https://raw.githubusercontent.com/Palchueck/imagenes-dentista/main/imagenes/${primeraPalabra}Dental.avif`}
                  delay={0.1 * index}
                />
              );
            })}
          </div>
        </div>
      </div>
    </section>
  );
};

export default Servicios;