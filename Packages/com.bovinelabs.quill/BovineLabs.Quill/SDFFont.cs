// <copyright file="SDFFont.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill
{
    using Unity.Collections;
    using Unity.Mathematics;

    internal readonly struct SDFCharacter
    {
        public readonly char Character;
        public readonly float Width;

        public SDFCharacter(
            char character, int x, int y, int width, int height, int originX, int originY, int advance, int textureWidth, int textureHeight, float defaultSize)
        {
            var texSize = new float2(textureWidth, textureHeight);

            this.Character = character;
            var uvMin = new float2(x, y) / texSize;
            var uvMax = new float2(x + width, y + height) / texSize;

            // UV (0,0) is at the bottom-left in Unity
            this.UVTopLeft = new float2(uvMin.x, 1.0f - uvMin.y);
            this.UVBottomRight = new float2(uvMax.x, 1.0f - uvMax.y);

            var pivot = new float2(-originX, originY);
            this.VertexTopLeft = (pivot + new float2(0, 0)) / defaultSize;
            this.VertexBottomRight = (pivot + new float2(width, -height)) / defaultSize;
            this.Width = advance / defaultSize;
        }

        public float2 UVTopLeft { get; }

        public float2 UVTopRight => new(this.UVBottomRight.x, this.UVTopLeft.y);

        public float2 UVBottomLeft => new(this.UVTopLeft.x, this.UVBottomRight.y);

        public float2 UVBottomRight { get; }

        public float2 VertexTopLeft { get; }

        public float2 VertexTopRight => new(this.VertexBottomRight.x, this.VertexTopLeft.y);

        public float2 VertexBottomLeft => new(this.VertexTopLeft.x, this.VertexBottomRight.y);

        public float2 VertexBottomRight { get; }
    }

    internal static class SDFFont
    {
        private const int Width = 1024;
        private const int Height = 128;
        private const int Size = 32;

        // https://evanw.github.io/font-texture-generator/
        public static readonly SDFCharacter[] Characters =
        {
            new(' ', 414, 79, 12, 12, 6, 6, 19, Width, Height, Size),
            new('!', 669, 44, 16, 35, -2, 29, 19, Width, Height, Size),
            new('"', 258, 79, 23, 20, 2, 29, 19, Width, Height, Size),
            new('#', 919, 0, 30, 35, 5, 29, 19, Width, Height, Size),
            new('$', 231, 0, 26, 38, 3, 30, 19, Width, Height, Size),
            new('%', 393, 0, 31, 36, 6, 29, 19, Width, Height, Size),
            new('&', 424, 0, 31, 36, 5, 29, 19, Width, Height, Size),
            new('\'', 281, 79, 16, 20, -2, 29, 19, Width, Height, Size),
            new('(', 115, 0, 22, 40, 1, 29, 19, Width, Height, Size),
            new(')', 137, 0, 22, 40, 1, 29, 19, Width, Height, Size),
            new('*', 159, 79, 27, 26, 4, 30, 19, Width, Height, Size),
            new('+', 186, 79, 27, 26, 4, 24, 19, Width, Height, Size),
            new(',', 240, 79, 18, 21, -1, 10, 19, Width, Height, Size),
            new('-', 359, 79, 23, 15, 2, 16, 19, Width, Height, Size),
            new('.', 315, 79, 17, 17, -1, 11, 19, Width, Height, Size),
            new('/', 500, 44, 25, 35, 3, 29, 19, Width, Height, Size),
            new('0', 569, 0, 27, 36, 4, 29, 19, Width, Height, Size),
            new('1', 649, 44, 20, 35, 2, 29, 19, Width, Height, Size),
            new('2', 313, 44, 27, 35, 4, 29, 19, Width, Height, Size),
            new('3', 758, 0, 26, 36, 4, 29, 19, Width, Height, Size),
            new('4', 60, 44, 29, 35, 5, 29, 19, Width, Height, Size),
            new('5', 448, 44, 26, 35, 3, 29, 19, Width, Height, Size),
            new('6', 596, 0, 27, 36, 4, 29, 19, Width, Height, Size),
            new('7', 340, 44, 27, 35, 4, 29, 19, Width, Height, Size),
            new('8', 623, 0, 27, 36, 4, 29, 19, Width, Height, Size),
            new('9', 650, 0, 27, 36, 4, 29, 19, Width, Height, Size),
            new(':', 861, 44, 16, 30, -2, 23, 19, Width, Height, Size),
            new(';', 711, 44, 18, 34, 0, 23, 19, Width, Height, Size),
            new('<', 77, 79, 27, 28, 4, 25, 19, Width, Height, Size),
            new('=', 213, 79, 27, 21, 4, 22, 19, Width, Height, Size),
            new('>', 104, 79, 27, 28, 4, 25, 19, Width, Height, Size),
            new('?', 784, 0, 26, 36, 3, 29, 19, Width, Height, Size),
            new('@', 200, 0, 31, 38, 6, 29, 19, Width, Height, Size),
            new('A', 949, 0, 30, 35, 5, 29, 19, Width, Height, Size),
            new('B', 89, 44, 28, 35, 4, 29, 19, Width, Height, Size),
            new('C', 513, 0, 28, 36, 4, 29, 19, Width, Height, Size),
            new('D', 117, 44, 28, 35, 4, 29, 19, Width, Height, Size),
            new('E', 474, 44, 26, 35, 3, 29, 19, Width, Height, Size),
            new('F', 525, 44, 25, 35, 2, 29, 19, Width, Height, Size),
            new('G', 541, 0, 28, 36, 4, 29, 19, Width, Height, Size),
            new('H', 367, 44, 27, 35, 4, 29, 19, Width, Height, Size),
            new('I', 625, 44, 24, 35, 2, 29, 19, Width, Height, Size),
            new('J', 550, 44, 25, 35, 4, 29, 19, Width, Height, Size),
            new('K', 145, 44, 28, 35, 3, 29, 19, Width, Height, Size),
            new('L', 575, 44, 25, 35, 2, 29, 19, Width, Height, Size),
            new('M', 173, 44, 28, 35, 4, 29, 19, Width, Height, Size),
            new('N', 394, 44, 27, 35, 4, 29, 19, Width, Height, Size),
            new('O', 455, 0, 29, 36, 5, 29, 19, Width, Height, Size),
            new('P', 421, 44, 27, 35, 3, 29, 19, Width, Height, Size),
            new('Q', 38, 0, 29, 42, 5, 29, 19, Width, Height, Size),
            new('R', 201, 44, 28, 35, 3, 29, 19, Width, Height, Size),
            new('S', 677, 0, 27, 36, 4, 29, 19, Width, Height, Size),
            new('T', 229, 44, 28, 35, 4, 29, 19, Width, Height, Size),
            new('U', 257, 44, 28, 35, 4, 29, 19, Width, Height, Size),
            new('V', 979, 0, 30, 35, 5, 29, 19, Width, Height, Size),
            new('W', 888, 0, 31, 35, 6, 29, 19, Width, Height, Size),
            new('X', 0, 44, 30, 35, 5, 29, 19, Width, Height, Size),
            new('Y', 30, 44, 30, 35, 5, 29, 19, Width, Height, Size),
            new('Z', 285, 44, 28, 35, 4, 29, 19, Width, Height, Size),
            new('[', 159, 0, 21, 40, 0, 29, 19, Width, Height, Size),
            new('\\', 600, 44, 25, 35, 3, 29, 19, Width, Height, Size),
            new(']', 180, 0, 20, 40, 1, 29, 19, Width, Height, Size),
            new('^', 131, 79, 28, 26, 4, 29, 19, Width, Height, Size),
            new('_', 382, 79, 32, 14, 6, 3, 19, Width, Height, Size),
            new('`', 297, 79, 18, 17, -1, 31, 19, Width, Height, Size),
            new('a', 784, 44, 26, 30, 4, 23, 19, Width, Height, Size),
            new('b', 285, 0, 27, 37, 4, 30, 19, Width, Height, Size),
            new('c', 810, 44, 26, 30, 3, 23, 19, Width, Height, Size),
            new('d', 312, 0, 27, 37, 4, 30, 19, Width, Height, Size),
            new('e', 757, 44, 27, 30, 4, 23, 19, Width, Height, Size),
            new('f', 704, 0, 27, 36, 4, 30, 19, Width, Height, Size),
            new('g', 257, 0, 28, 37, 4, 23, 19, Width, Height, Size),
            new('h', 810, 0, 26, 36, 3, 30, 19, Width, Height, Size),
            new('i', 836, 0, 26, 36, 3, 30, 19, Width, Height, Size),
            new('j', 0, 0, 23, 44, 4, 30, 19, Width, Height, Size),
            new('k', 731, 0, 27, 36, 3, 30, 19, Width, Height, Size),
            new('l', 862, 0, 26, 36, 3, 30, 19, Width, Height, Size),
            new('m', 909, 44, 29, 29, 5, 23, 19, Width, Height, Size),
            new('n', 995, 44, 26, 29, 3, 23, 19, Width, Height, Size),
            new('o', 729, 44, 28, 30, 4, 23, 19, Width, Height, Size),
            new('p', 339, 0, 27, 37, 4, 23, 19, Width, Height, Size),
            new('q', 366, 0, 27, 37, 4, 23, 19, Width, Height, Size),
            new('r', 52, 79, 25, 29, 2, 23, 19, Width, Height, Size),
            new('s', 836, 44, 25, 30, 3, 23, 19, Width, Height, Size),
            new('t', 685, 44, 26, 34, 4, 28, 19, Width, Height, Size),
            new('u', 0, 79, 26, 29, 3, 23, 19, Width, Height, Size),
            new('v', 938, 44, 29, 29, 5, 23, 19, Width, Height, Size),
            new('w', 877, 44, 32, 29, 6, 23, 19, Width, Height, Size),
            new('x', 967, 44, 28, 29, 4, 23, 19, Width, Height, Size),
            new('y', 484, 0, 29, 36, 5, 23, 19, Width, Height, Size),
            new('z', 26, 79, 26, 29, 3, 23, 19, Width, Height, Size),
            new('{', 67, 0, 24, 40, 2, 29, 19, Width, Height, Size),
            new('|', 23, 0, 15, 44, -2, 30, 19, Width, Height, Size),
            new('}', 91, 0, 24, 40, 2, 29, 19, Width, Height, Size),
            new('~', 332, 79, 27, 16, 4, 19, 19, Width, Height, Size),
        };

        public static NativeArray<SDFCharacter> CreateCharacterArray(Allocator allocator)
        {
            var characters = new NativeArray<SDFCharacter>(128, allocator);

            var questionMark = Characters[0];

            for (var i = 0; i < Characters.Length; i++)
            {
                if (Characters[i].Character == '?')
                {
                    questionMark = Characters[i];
                    break;
                }
            }

            for (var i = 0; i < characters.Length; i++)
            {
                characters[i] = questionMark;
            }

            foreach (var character in Characters)
            {
                characters[character.Character] = character;
            }

            return characters;
        }
    }
}
