import { useEffect, useState } from "react";
import { getSensorReadings, type ReadingHistoryResponse } from "../api";
import { SensorResponse, sensorTypeLabel, formatDateTime } from "../types";

interface Props {
  sensor: SensorResponse;
  onClose: () => void;
}

type Window = "1h" | "6h" | "24h" | "7d" | "30d";

export default function SensorDetail({ sensor, onClose }: Props) {
  const [data, setData] = useState<ReadingHistoryResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [window, setWindow] = useState<Window>("24h");

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const result = await getSensorReadings(sensor.id, sensor.type, window);
        setData(result);
      } catch (err) {
        setError(
          err instanceof Error ? err.message : "Failed to load readings",
        );
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [sensor.id, window]);

  if (loading) {
    return (
      <div className="sensor-detail">
        <div className="sensor-detail-header">
          <button className="back-btn" onClick={onClose}>
            ← Back
          </button>
          <h2>{sensorTypeLabel(sensor.type)}</h2>
        </div>
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 12,
            color: "var(--text-1)",
          }}
        >
          <div className="spinner" /> Loading readings…
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="sensor-detail">
        <div className="sensor-detail-header">
          <button className="back-btn" onClick={onClose}>
            ← Back
          </button>
          <h2>{sensorTypeLabel(sensor.type)}</h2>
        </div>
        <div className="msg msg-error">{error}</div>
      </div>
    );
  }

  if (!data || data.readings.length === 0) {
    return (
      <div className="sensor-detail">
        <div className="sensor-detail-header">
          <button className="back-btn" onClick={onClose}>
            ← Back
          </button>
          <h2>{sensorTypeLabel(sensor.type)}</h2>
        </div>
        <div className="empty">No readings available</div>
      </div>
    );
  }

  const readings = data.readings;
  const values = readings.map((r) => r.value);
  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = max - min || 1;

  // Graph dimensions
  const graphWidth = 600;
  const graphHeight = 250;
  const padding = 40;
  const plotWidth = graphWidth - padding * 2;
  const plotHeight = graphHeight - padding * 2;

  // Points for the line graph
  const points = readings.map((r, i) => {
    const x = padding + (i / (readings.length - 1 || 1)) * plotWidth;
    const y = padding + plotHeight - ((r.value - min) / range) * plotHeight;
    return { x, y, value: r.value, time: r.recordedAt };
  });

  const pathData =
    points.length > 0
      ? points.map((p, i) => `${i === 0 ? "M" : "L"} ${p.x} ${p.y}`).join(" ")
      : "";

  return (
    <div className="sensor-detail">
      <div className="sensor-detail-header">
        <button className="back-btn" onClick={onClose}>
          ← Back
        </button>
        <h2>{sensorTypeLabel(sensor.type)}</h2>
      </div>

      {/* Window selector */}
      <div className="window-selector">
        {(["1h", "6h", "24h", "7d", "30d"] as Window[]).map((w) => (
          <button
            key={w}
            className={`btn btn-sm ${window === w ? "btn-primary" : "btn-ghost"}`}
            onClick={() => setWindow(w)}
          >
            {w}
          </button>
        ))}
      </div>

      {/* Graph */}
      <div className="graph-container">
        <svg width={graphWidth} height={graphHeight} className="graph">
          {/* Y-axis */}
          <line
            x1={padding}
            y1={padding}
            x2={padding}
            y2={graphHeight - padding}
            stroke="var(--text-2)"
            strokeWidth="1"
          />
          {/* X-axis */}
          <line
            x1={padding}
            y1={graphHeight - padding}
            x2={graphWidth - padding}
            y2={graphHeight - padding}
            stroke="var(--text-2)"
            strokeWidth="1"
          />

          {/* Y-axis labels */}
          <text
            x={padding - 10}
            y={padding + 5}
            textAnchor="end"
            fontSize="12"
            fill="var(--text-2)"
          >
            {max.toFixed(1)}
          </text>
          <text
            x={padding - 10}
            y={graphHeight - padding + 5}
            textAnchor="end"
            fontSize="12"
            fill="var(--text-2)"
          >
            {min.toFixed(1)}
          </text>

          {/* Grid lines */}
          {[0.25, 0.5, 0.75].map((frac) => (
            <line
              key={frac}
              x1={padding}
              y1={padding + plotHeight * (1 - frac)}
              x2={graphWidth - padding}
              y2={padding + plotHeight * (1 - frac)}
              stroke="var(--bg-2)"
              strokeWidth="1"
              strokeDasharray="2,2"
            />
          ))}

          {/* Line graph */}
          <path
            d={pathData}
            fill="none"
            stroke="var(--accent)"
            strokeWidth="2"
          />

          {/* Data points */}
          {points.map((p, i) => (
            <circle key={i} cx={p.x} cy={p.y} r="3" fill="var(--accent)" />
          ))}
        </svg>
        <div className="graph-unit">{data.unit}</div>
      </div>

      {/* Readings list */}
      <div className="readings-list">
        <h3>Readings</h3>
        <div className="readings-table">
          <div className="readings-header">
            <div>Time</div>
            <div className="readings-value">Value ({data.unit})</div>
          </div>
          {readings.map((reading) => (
            <div key={reading.id} className="readings-row">
              <div>{formatDateTime(reading.recordedAt)}</div>
              <div className="readings-value">{reading.value.toFixed(2)}</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
