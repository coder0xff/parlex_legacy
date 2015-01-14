#include "Unicode.h"
#include <vector>
#include <memory>
#include <map>

typedef std::vector<char32_t> Text;

namespace Parlex {

#define READ_CHARACTER_SET(name, character_set) \
		static bool name(Text codepoints, int& position) { \
			bool matches = position < codepoints.size() && character_set.count(codepoints[position]); \
			if (matches) position++; \
			return matches; \
		}

	READ_CHARACTER_SET(ReadLetter, Unicode::Letters);
	READ_CHARACTER_SET(ReadNumber, Unicode::Numbers);
	READ_CHARACTER_SET(ReadDecimalDigit, Unicode::DecimalDigits);
	READ_CHARACTER_SET(ReadHexidecimalDigit, Unicode::HexidecimalDigits);
	READ_CHARACTER_SET(ReadAlphaNumeric, Unicode::Alphanumeric);
	READ_CHARACTER_SET(ReadWhiteSpace, Unicode::WhiteSpace);

#undef READ_CHARACTER_SET

	static bool ReadCharacter(Text codepoints, int& position) {
		bool matches = position < codepoints.size();
		if (matches) position++;
		return matches;
	}

	static bool ReadCharacter(Text codepoints, int& position, char32_t codepoint) {
		bool matches = position < codepoints.size() && codepoints[position] == codepoint;
		if (matches) position++;
		return matches;
	}

	static bool TestCharacter(Text codepoints, int position, char32_t codepoint) {
		bool matches = position < codepoints.size() && codepoints[position] == codepoint;
		return matches;
	}

	static int ReadWhiteSpaces(Text codepoints, int& position) {
		int start = position;
		while (position < codepoints.size() && Unicode::WhiteSpace.count(codepoints[position])) {
			position++;
		}
		return position - start;
	}

	static bool ReadNonDoubleQuote(Text codepoints, int& position, char32_t& result) {
		if (position < codepoints.size()) {
			result = codepoints[position];
			return result != '"';
		}
		return false;
	}

	static bool ReadDoubleQuote(Text codepoints, int& position) {
		bool matches = position < codepoints.size() && codepoints[position] == '"';
		if (matches) position++;
		return matches;
	}

	static bool ReadNonDoubleQuoteNonBackSlash(Text codepoints, int& position, char32_t& result) {
		if (position < codepoints.size()) {
			result = codepoints[position];
			return result != '"' && result != '\\';
		}
		return false;
	}

	static std::map<char32_t, char32_t> escapeTable = { { 'a', '\a' }, { 'b', '\b' }, { 'f', '\f' }, { 'n', '\n' }, { 'r', '\r' }, { 't', '\t' }, { '\\', '\\' }, { '\'', '\'' }, { '"', '"' }, { '?', '?' } };

	static bool ReadSimpleEscapeSequence(Text codepoints, int& position, char32_t& result) {
		int tempPosition = position;
		if (ReadCharacter(codepoints, tempPosition, '\\')) {
			if (tempPosition < codepoints.size()) {
				auto i = escapeTable.find(codepoints[tempPosition]);
				if (i != escapeTable.end()) {
					result = i->second;
					position = tempPosition;
					return true;
				}
			}
		}
		return false;
	}

	static bool ReadUnicodeEscapeSequence(Text codepoints, int& position, char32_t& result) {
		int tempPosition = position;
		if (ReadCharacter(codepoints, tempPosition, '\\') && ReadCharacter(codepoints, tempPosition, 'x')) {
			int digitCount = 0;
			char32_t accumulator = 0;
			while (tempPosition < codepoints.size() && digitCount < 6) {
				char32_t c = codepoints[tempPosition];
				if (c >= 'a' && c <= 'f') c -= 'a' - 10;
				else if (c >= 'A' && c <= 'F') c -= 'A' - 10;
				else if (c >= '0' && c <= '9' && c >= 'A') c -= '0';
				else break;
				accumulator *= 16;
				accumulator += c;
				tempPosition++;
			}
			if (digitCount == 6) {
				position = tempPosition;
				result = accumulator;
				return true;
			}
			return false;
		}
	}

	static bool ReadStringLiteral(Text codepoints, int& position, std::u32string& result) {
		int tempPosition = position;
		result.clear();
		if (!ReadDoubleQuote(codepoints, tempPosition)) return false;

		while (tempPosition < codepoints.size()) {
			char32_t c = codepoints[tempPosition];
			if (c == '"') {
				position = tempPosition;
				return true;
			}
			if (c == '\\') {
				if (!ReadSimpleEscapeSequence(codepoints, tempPosition, c)) {
					ReadUnicodeEscapeSequence(codepoints, tempPosition, c);
				}
			}
			result.push_back(c);
		}
		return false;
	}

}