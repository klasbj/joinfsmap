import React, { Component } from 'react';
import { Fetcher } from './Fetcher';
import { Map, TileLayer, Marker, Popup } from 'react-leaflet';
import './App.css';


interface Status {
  timeStamp: string,
  atcs: Atc[],
  pilots: Pilot[]
}

interface Position {
  latitude: number,
  longitude: number,
  altitude: number
}

interface Entity {
  callsign: string,
  userName: string,
  position: Position,
  key: string
}

interface Atc extends Entity {
  frequency: number[]
}

interface Pilot extends Entity {
  groundSpeed: number,
  heading: number,
  aircraftType: string,
  aircraftTypeShort: string,
  trail: { timeStamp: string, position: Position }
}

export function App() {
  return (
    <Fetcher<Status> src="api/ServerStatus">
      {(data, loading, error) => {
        if (loading) {
          return <p>Loading...</p>
        }

        if (error !== null || data === null) {
          return <p>Error: {error}</p>
        }


        return (<div className="App">
          <div className="MapContainer">
            <Map center={[59.9, 15]} zoom={3}>
              <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                attribution="&copy; <a href=&quot;http://osm.org/copyright&quot;>OpenStreetMap</a> contributors" />
              {data.pilots.map(a =>
                <Marker key={a.key} position={[a.position.latitude, a.position.longitude]}>
                  <Popup>{a.userName} {a.aircraftTypeShort}</Popup>
                </Marker>)}
            </Map>
          </div>
          <div>
            <p>Status at {data.timeStamp}</p>
            <h3>ATC</h3>
            <ul>
              {data.atcs.map(a => <li key={a.key}>{a.callsign} {a.frequency} &mdash; {a.userName}</li>)}
            </ul>
            <h3>Pilots</h3>
            <ul>
              {data.pilots.map(a => <li key={a.key}>{a.callsign} {a.userName} {a.aircraftType}</li>)}
            </ul>
          </div>
        </div>);
      }}
    </Fetcher>
  );
}
