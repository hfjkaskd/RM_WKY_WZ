namespace Whitebox.Foundation
{
    // Native TiledToTypeConverter.AsLayerCount 0x191133c.
    // Original numeric ID rules, not designer-adjustable tuning.
    public static class OriginalTiledItemLayers
    {
        public static int AsLayerCount(int tiledId)
        {
            switch (tiledId)
            {
                case 123: case 124: case 133: case 134: case 303: case 304:
                case 305: case 306: case 354: case 32757:
                    return 0;
                case 22: case 25: case 28: case 32: case 88: case 91:
                case 92: case 93: case 94: case 95: case 98: case 150:
                case 152: case 159: case 160: case 167: case 188: case 204:
                case 226: case 227: case 228: case 229: case 253: case 293:
                case 350: case 394: case 32762: case 32764: case 32767:
                    return 2;
                case 23: case 29: case 86: case 101: case 102: case 103:
                case 104: case 105: case 151: case 168: case 241: case 311:
                case 312: case 313: case 330: case 340: case 366: case 376:
                case 377: case 378: case 381: case 382: case 391: case 392:
                case 401: case 32740: case 32744: case 32756: case 32758:
                    return 3;
                case 24: case 53: case 71: case 73: case 75: case 77:
                case 79: case 157: case 173: case 175: case 194: case 271:
                case 273: case 301: case 310: case 327: case 347: case 349:
                case 367: case 383: case 398: case 32745:
                    return 4;
                case 55: case 57: case 108: case 109: case 110: case 131:
                case 179: case 261: case 287: case 288: case 289: case 297:
                case 298: case 299: case 308: case 393:
                    return 5;
                case 126: case 127: case 136: case 137: case 138: case 234:
                case 281: case 379:
                    return 6;
                case 171: case 371:
                    return 7;
                case 106: case 277: case 279:
                    return 8;
                case 351:
                    return 9;
                case 51:
                    return 11;
                case 201:
                    return 15;
                default: return 1;
            }
        }
    }
}
