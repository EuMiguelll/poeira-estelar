"""Gera a trilha sonora de Poeira Estelar (synthwave em loop) como WAV.

Uso: uv run --with numpy python Tools/gerar_musica.py Assets/Audio/Music/trilha_poeira_estelar.wav
"""
import sys
import wave

import numpy as np

SR = 44100
BPM = 100
BEAT = 60 / BPM
BAR = 4 * BEAT
BARS = 32

# Am - F - C - G (graus em semitons a partir de A2 = 110 Hz)
PROGRESSION = [
    [0, 3, 7],    # Am
    [-4, 0, 3],   # F
    [3, 7, 10],   # C
    [-2, 2, 5],   # G
]
ROOTS = [0, -4, 3, -2]


def hz(semitones, base=110.0):
    return base * 2 ** (semitones / 12)


def envelope(n, attack, release):
    env = np.ones(n)
    a = max(1, int(attack * SR))
    r = max(1, int(release * SR))
    env[:a] = np.linspace(0, 1, a)
    env[-r:] *= np.linspace(1, 0, r)
    return env


def saw(freq, n, detune=0.0):
    t = np.arange(n) / SR
    out = np.zeros(n)
    for d in (-detune, 0, detune):
        f = freq * (1 + d)
        out += 2 * ((t * f) % 1) - 1
    return out / 3


def triangle(freq, n):
    t = np.arange(n) / SR
    return 2 * np.abs(2 * ((t * freq) % 1) - 1) - 1


def pulse(freq, n, width=0.25):
    t = np.arange(n) / SR
    return np.where((t * freq) % 1 < width, 1.0, -1.0)


def lowpass(x, cutoff):
    alpha = 1 - np.exp(-2 * np.pi * cutoff / SR)
    y = np.zeros_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc += alpha * (v - acc)
        y[i] = acc
    return y


def add(buf, start, sig):
    i = int(start * SR)
    end = min(len(buf), i + len(sig))
    buf[i:end] += sig[: end - i]


def render():
    total = int(BARS * BAR * SR)
    pad = np.zeros(total)
    bass = np.zeros(total)
    arp = np.zeros(total)
    drums = np.zeros(total)
    rng = np.random.default_rng(7)

    for bar in range(BARS):
        chord = PROGRESSION[bar % 4]
        root = ROOTS[bar % 4]
        t0 = bar * BAR

        # Pad: acorde inteiro, ataque lento.
        n = int(BAR * SR)
        chord_sig = sum(saw(hz(c + 12), n, detune=0.004) for c in chord) / 3
        add(pad, t0, chord_sig * envelope(n, 0.4, 0.5))

        # Baixo: colcheias na fundamental, oitava abaixo.
        for step in range(8):
            n = int(BEAT / 2 * SR)
            sig = saw(hz(root - 12), n) * envelope(n, 0.005, 0.12)
            add(bass, t0 + step * BEAT / 2, sig * (1.0 if step % 2 == 0 else 0.7))

        # Arpejo: semicolcheias; segunda metade da música sobe uma oitava.
        if bar >= 4:
            notes = chord + [chord[0] + 12, chord[1] + 12, chord[2] + 12, chord[1] + 12, chord[0] + 12]
            octave = 24 if bar >= 20 else 12
            for step in range(16):
                n = int(BEAT / 4 * SR)
                note = notes[step % len(notes)] + octave
                sig = (triangle(hz(note), n) * 0.7 + pulse(hz(note), n) * 0.3) * envelope(n, 0.003, 0.08)
                add(arp, t0 + step * BEAT / 4, sig)

        # Bateria: entra no compasso 8 (bumbo, caixa e chimbal).
        if bar >= 8 and bar != 19:
            for beat in range(4):
                n = int(0.25 * SR)
                t = np.arange(n) / SR
                kick = np.sin(2 * np.pi * (50 + 90 * np.exp(-t * 30)) * t) * np.exp(-t * 12)
                add(drums, t0 + beat * BEAT, kick * 0.9)
                if beat in (1, 3):
                    snare = rng.uniform(-1, 1, n) * np.exp(-t * 22)
                    add(drums, t0 + beat * BEAT, lowpass(snare, 3500) * 0.45)
                hat_n = int(0.05 * SR)
                ht = np.arange(hat_n) / SR
                hat = rng.uniform(-1, 1, hat_n) * np.exp(-ht * 90)
                add(drums, t0 + beat * BEAT + BEAT / 2, (hat - lowpass(hat, 6000)) * 0.35)

    pad = lowpass(pad, 1400)
    bass = lowpass(bass, 600)
    mix = pad * 0.30 + bass * 0.32 + arp * 0.16 + drums * 0.45

    # Eco simples (delay com realimentação) que dá sensação de espaço.
    delay = int(BEAT * 0.75 * SR)
    wet = np.zeros(total + delay * 6)
    wet[:total] = arp * 0.16
    for i in range(1, 6):
        wet[delay * i: delay * i + total] += arp * 0.16 * (0.45 ** i)
    tail = wet[total:]
    wet = wet[:total]
    wet[: len(tail)] += tail  # dobra a cauda no começo: loop sem emenda
    mix += wet * 0.5

    left = mix + np.roll(wet, int(0.012 * SR)) * 0.2
    right = mix + np.roll(wet, int(0.019 * SR)) * 0.2
    stereo = np.stack([left, right], axis=1)
    stereo /= np.max(np.abs(stereo)) * 1.12
    return stereo


def main(path):
    data = (render() * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    print(path, f"{len(data) / SR:.1f}s")


if __name__ == "__main__":
    main(sys.argv[1])
