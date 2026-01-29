#!/usr/bin/python

# MIT License

# Copyright (c) 2026 Fundale

# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:

# The above copyright notice and this permission notice shall be included in all
# copies or substantial portions of the Software.

# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
# SOFTWARE.

# ArtNet Noisinator script written in Python for HNode stress testing.

import socket
import time

# Begin Config Options

ARTNET_RATE = 45
ARTNET_IP = "127.0.0.1"
ARTNET_PORT = 6454

ARTNET_UNIVERSES = 28
ARTNET_CHANNELS = 512

BYTESWAP = True

# End Config Options

ARTNET_HEADER = bytes([
    *bytes("Art-Net", "utf-8"), 0x0, #Art-Net\0: header
    0x0, 0x50, #mode: data
    0x0, 0x0e, #version: 14
    ])

ARTNET_SEQUENCE = 0x0

ARTNET_BSINE = 0x55
ARTNET_BCOSN = 0xAA

ARTNET_ZERO = 0x0
ARTNET_ONE = 0xff

ARTNET_SINE = bytes()
ARTNET_COSN = bytes()

ARTNET_ZEROS = bytes()
ARTNET_ONES = bytes()

ARTNET_SOCKET = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

CLOCK_SECONDS = (1000 / ARTNET_RATE) / 1000
RUNNING = True

def generate_artnet_noise(universe):
    data1 = ARTNET_SINE if ARTNET_SEQUENCE % 2 == 0 else ARTNET_COSN
    data2 = ARTNET_ONES if ARTNET_SEQUENCE % 2 == 0 else ARTNET_ZEROS

    data = data2 if BYTESWAP else data1

    return bytes([
            *ARTNET_HEADER,

            ARTNET_SEQUENCE, #sequence
            0x0, #physical

            universe % 256, universe // 256, #universe
            ARTNET_CHANNELS // 256, ARTNET_CHANNELS % 256, #length
            *data #data
            ])

try:
    for indx in range(ARTNET_CHANNELS):
        ARTNET_SINE = bytes([
            *bytes(ARTNET_SINE),
            ARTNET_BSINE
        ])
        ARTNET_COSN = bytes([
            *bytes(ARTNET_COSN),
            ARTNET_BCOSN
        ])

    for indx in range(ARTNET_CHANNELS // 2):
        ARTNET_ZEROS = bytes([
            *bytes(ARTNET_ZEROS),
            ARTNET_ZERO,
            ARTNET_ONE
        ])

        ARTNET_ONES = bytes([
            *bytes(ARTNET_ONES),
            ARTNET_ONE,
            ARTNET_ZERO
        ])
    
    while RUNNING:
        for indx in range(ARTNET_UNIVERSES):
            ARTNET_SOCKET.sendto(generate_artnet_noise(indx), (ARTNET_IP, ARTNET_PORT))
        
        ARTNET_SEQUENCE = (ARTNET_SEQUENCE + 1) % 256

        time.sleep(CLOCK_SECONDS)
except KeyboardInterrupt as e:
    RUNNING = False
    exit(0)