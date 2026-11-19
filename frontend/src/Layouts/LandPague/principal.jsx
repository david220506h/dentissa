// import React, { useState, useEffect } from 'react';
import './principal.css';
import Servicios from './nuestrosServicios';
import { FaCalendarAlt, FaPhoneAlt, FaStar} from 'react-icons/fa';
import Nav from '../../components/nav';
import { Star,User, Medal, HistoricShieldAlt } from 'iconoir-react';


function Principal({ usuario, setUsuario, onVerDashboard }) {
    

    return (
        <div className="principal-container">
            <Nav setUsuario={setUsuario} usuario={usuario} onVerDashboard={onVerDashboard}/>  
            <div className='content-principal'> 
                <div className='textos-principal'>
                    <div className='frase-container'>
                        <svg className="frase-svg" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="#d75078">
                            <path d="M11.352 2.005a2.234 2.234 0 0 0-2.168 1.693l-.49 1.963a4.17 4.17 0 0 1-3.032 3.032l-1.964.491a2.234 2.234 0 0 0 0 4.335l1.964.491a4.17 4.17 0 0 1 3.031 3.032l.491 1.964a2.234 2.234 0 0 0 4.335 0l.491-1.964a4.17 4.17 0 0 1 3.032-3.032l1.964-.49a2.234 2.234 0 0 0 0-4.336l-1.964-.49A4.17 4.17 0 0 1 14.01 5.66l-.49-1.963a2.234 2.234 0 0 0-2.168-1.693m-.593 2.086a.61.61 0 0 1 1.185 0l.491 1.964a5.79 5.79 0 0 0 4.213 4.213l1.964.491a.61.61 0 0 1 0 1.185l-1.964.491a5.79 5.79 0 0 0-4.213 4.213l-.49 1.964a.61.61 0 0 1-1.186 0l-.49-1.964a5.79 5.79 0 0 0-4.214-4.213l-1.963-.49a.61.61 0 0 1 0-1.186l1.963-.49a5.79 5.79 0 0 0 4.214-4.214zm8.307 11.35a.583.583 0 0 0-1.132 0l-.201.806a2.04 2.04 0 0 1-1.486 1.486l-.805.201a.583.583 0 0 0 0 1.132l.805.201a2.04 2.04 0 0 1 1.486 1.486l.201.805a.583.583 0 0 0 1.132 0l.201-.805a2.04 2.04 0 0 1 1.486-1.486l.805-.201a.583.583 0 0 0 0-1.132l-.805-.201a2.04 2.04 0 0 1-1.486-1.486z"/>
                        </svg>
                        <p>Clínica Dental de Confianza</p>
                    </div>
                    <h1 className='eslogan'>Tu Sonrisa <br /> Perfecta <br />Comienza Aquí</h1>
                    <p className='primer-informacion'> Más de 15 años cuidando sonrisas con tecnología de <br /> vanguardia y los mejores especialistas en salud <br /> dental</p>
                    <div className='botones-cita-contacto'>
                        <button className='button-agendar-cita'> <FaCalendarAlt /> Agendar Cita Ahora</button>
                        <a href="tel:+5255123456789" className="button-contactar">
                        <FaPhoneAlt /> +52 1 742 121 9199
                        </a>                    </div>
                    <div className='estadisticas-resumen'>
                        <div>
                            <p>15+</p>
                            <p>Años de Experiencia</p>
                        </div>
                        <div>
                            <p>5K</p>
                            <p>Pacientes Satisfechos</p>
                        </div>
                        <div>
                            <p>9.5 <FaStar /></p>
                            <p>Rating</p>
                        </div>
                    </div>
                </div>       
                <img className='imagen-carita-feliz' src="https://decogaleria.mx/cdn/shop/files/D_NQ_NP_2X_608590-MLM71845458112_092023-F_3000x.jpg?v=1717714552" alt="cara feliz " />
                
            </div>
            <hr style={{margin: '50px 0'}}/>
            <div className='estadisticas'>
                <div>
                    <p><Medal/></p>
                    <p>15+</p>
                    <p>Años de Experiencia</p>
                </div>
                <div>
                    <p><User/></p>
                    <p>5,000+</p>
                    <p>Pacientes Felices</p>
                </div>
                <div>
                    <p><Star/></p>
                    <p>4.5</p>
                    <p>Calificacion</p>
                </div>
                <div>
                    <p ><HistoricShieldAlt /></p>
                    <p>100%</p>
                    <p>Garantía</p>
                </div>
            </div>
            <hr style={{margin: '50px 0',}}/>
            <Servicios />

        </div>
    );
}
export default Principal;