"""Explicit BCL/table ports for executing original inventory-time reset bodies.

The original ResetTimes, scalar writes/packing, BitArrayOperations loops,
signed-byte serialization/trimming and SaveAndSyncData(false) execute in ELF.
Only diagnostic logging is skipped. This helper supplies metadata, BCL arrays,
StringBuilder/string operations and the typed storage boundary, not reset rules.
"""
import struct


class InventoryTimeResetNative:
    def __init__(self, u, r, write, failure):
        self.u, self.r, self.write, self.failure = u, r, write, failure
        self.builder, self.builder_class, self.statics = 0x10158000, 0x10159000, 0x1015a000
        self.array = 0x10160000
        self.builder_slot = self.get(0x539c6c0)
        self.literal_slots = {self.get(0x5372478): 'RemainingBoosterTimes'}
        self.key_slots, self.key_names = {}, {}
        for i, name in enumerate(('RET', 'TET', 'LET', 'Chest', 'MultiplierEndTimes', 'Feature2', 'ByteArrayKeyValue12')):
            slot, pointer = 0x10140000 + i * 0x100, 0x10148000 + i * 0x100
            self.put(slot, pointer); self.key_slots[name] = slot; self.key_names[pointer] = name
        self.skips = {
            0x28e728c: (0x28e7358, 24, 'RET'), 0x28e7400: (0x28e74cc, 24, 'TET'),
            0x28e7574: (0x28e7640, 24, 'LET'), 0x28e5218: (0x28e52e4, 24, 'Chest'),
            0x28e771c: (0x28e7844, 26, 'MultiplierEndTimes'),
            0x28e8640: (0x28e870c, 24, 'MultiplierEndTimes'),
            0x28e8f94: (0x28e9060, 25, 'Feature2'),
        }
        self.addresses = tuple(self.skips) + (0x2657978, 0x32ef9dc, 0x32efcb8, 0xffd9d0,
            0x1cc4b9c, 0x1cc5f04, 0x1cc5eb4, 0x20002000, 0x1d70bd8, 0x25d0324,
            0x25e514c, 0x25e50ac, 0x25e3900)
        self.put(self.builder, self.builder_class); self.put(self.builder_class + 0x168, 0x20002000)
        self.put(self.builder_class + 0x170, 0); self.put(self.statics, self.builder)
        self.prepare()

    def put(self, p, value, size=8):
        self.u.mem_write(p, (int(value) & ((1 << (size * 8)) - 1)).to_bytes(size, 'little'))

    def get(self, p, size=8, signed=False):
        return int.from_bytes(self.u.mem_read(p, size), 'little', signed=signed)

    def x(self, i): return self.u.reg_read(getattr(self.r, 'UC_ARM64_REG_X' + str(i)))

    def sx(self, i, value):
        self.u.reg_write(getattr(self.r, 'UC_ARM64_REG_X' + str(i)), int(value) & ((1 << 64) - 1))

    def returned(self, value=0):
        self.sx(0, value); self.u.reg_write(self.r.UC_ARM64_REG_PC, self.u.reg_read(self.r.UC_ARM64_REG_LR))

    @staticmethod
    def signed(value, bits=64):
        value &= (1 << bits) - 1
        return value - (1 << bits) if value >> (bits - 1) else value

    def prepare(self, negative_sign='-'):
        self.bitarrays, self.next_bitarray, self.next_string = {}, 0x10170000, 0x10180000
        self.text, self.negative_sign = '', negative_sign

    def initialize_metadata(self, slot, metadata):
        if slot == self.builder_slot: self.put(metadata + 0xb8, self.statics)
        if slot in self.literal_slots: self.key_names[metadata] = self.literal_slots[slot]

    def new_bits(self, bits):
        if bits is None: return 0
        pointer = self.next_bitarray; self.next_bitarray += 0x100
        self.bitarrays[pointer] = list(bits); self.put(pointer + 0x18, len(bits), 4)
        return pointer

    def new_string(self, text):
        if text is None: return 0
        raw = text.encode('utf-16-le'); pointer = self.next_string; self.next_string += ((len(raw) + 0x30 + 15) // 16) * 16
        assert self.next_string < 0x10200000
        self.put(pointer + 0x10, len(raw) // 2, 4); self.u.mem_write(pointer + 0x14, raw + b'\0\0')
        return pointer

    def string(self, pointer):
        if not pointer: return None
        return bytes(self.u.mem_read(pointer + 0x14, self.get(pointer + 0x10, 4) * 2)).decode('utf-16-le')

    def number(self, value): return str(value).replace('-', self.negative_sign)

    def handle(self, address):
        if address in self.skips:
            target, register, key = self.skips[address]
            self.sx(register, self.key_slots[key]); self.u.reg_write(self.r.UC_ARM64_REG_PC, target); return True
        if address == 0x2657978:
            self.returned(min(self.signed(self.x(0), 32), self.signed(self.x(1), 32))); return True
        if address == 0x32ef9dc:
            bits = self.bitarrays.get(self.x(0))
            if bits is None: self.failure('null')
            elif not 0 <= self.signed(self.x(1), 32) < len(bits): self.failure('bounds')
            else: bits[self.signed(self.x(1), 32)] = bool(self.x(2)); self.returned()
            return True
        if address == 0xffd9d0:
            length = self.x(1); assert length <= 8192, length
            self.u.mem_write(self.array, bytes(0x20 + length)); self.put(self.array + 0x18, length, 4)
            self.returned(self.array); return True
        if address == 0x32efcb8:
            bits = self.bitarrays.get(self.x(0))
            if bits is None: self.failure('null')
            else:
                data = bytearray((len(bits) + 7) // 8)
                for i, bit in enumerate(bits):
                    if bit: data[i // 8] |= 1 << (i % 8)
                if data: self.u.mem_write(self.x(1) + 0x20, bytes(data))
                self.returned()
            return True
        if address == 0x1cc4b9c: self.text = ''; self.returned(self.builder); return True
        if address == 0x1cc5f04:
            self.text += self.number(self.signed(self.x(1), 8)); self.returned(self.builder); return True
        if address == 0x1cc5eb4:
            self.text += chr(self.x(1) & 65535); self.returned(self.builder); return True
        if address == 0x20002000: self.returned(self.new_string(self.text)); return True
        if address == 0x1d70bd8:
            self.returned(self.string(self.x(0)) == self.string(self.x(1))); return True
        if address == 0x25d0324:
            assert self.x(0) == 12, self.x(0)
            self.returned(self.get(self.key_slots['ByteArrayKeyValue12'])); return True
        if address in (0x25e514c, 0x25e50ac, 0x25e3900):
            key = self.key_names.get(self.x(0))
            if key is None: return False
            kind = 'string' if address == 0x25e3900 else 'int' if address == 0x25e50ac else 'long'
            value = self.string(self.x(1)) if kind == 'string' else self.signed(self.x(1), 32 if kind == 'int' else 64)
            if self.write(kind, key, value): self.returned(1)
            return True
        return False
