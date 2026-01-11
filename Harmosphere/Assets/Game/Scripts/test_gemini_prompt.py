#!/usr/bin/env python3
"""
Simple Python script to test Gemini API prompt and response parsing
"""

import requests
import json

# API configuration
API_KEY = "AIzaSyAQTdy1nEgjCKzqhNCsw82YdOjOo70J0E8"
MODEL = "gemini-2.5-flash"
URL = f"https://generativelanguage.googleapis.com/v1beta/models/{MODEL}:generateContent?key={API_KEY}"

# System prompt (therapist persona)
SYSTEM_PROMPT = """You are a compassionate psychologist and mental health counselor.
Your role is to understand the concerns and emotions of university students.
Be empathetic, non-judgmental, and supportive."""

# Test prompt for intro options
TEST_PROMPT_1 = """Think about the most common concerns and worries that university students face today.
Please identify and list exactly 3 different common concerns that students often experience.

Format your response EXACTLY like this (using these delimiters):
OPTION_1|[first common concern]
OPTION_2|[second common concern]
OPTION_3|[third common concern]
WELCOME|[2-3 sentence welcoming message]

Example:
OPTION_1|Academic pressure and exam anxiety
OPTION_2|Social isolation and loneliness
OPTION_3|Work-life balance and burnout
WELCOME|Welcome to HarmoSphere! Many students struggle with these challenges. Let's explore what's on your mind today."""

def test_gemini_api():
    """Test Gemini API with the specified prompts"""

    # Combine system prompt with user prompt for now
    combined_prompt = f"""{SYSTEM_PROMPT}

{TEST_PROMPT_1}"""

    # Request payload
    payload = {
        "contents": [
            {
                "parts": [
                    {
                        "text": combined_prompt
                    }
                ]
            }
        ]
    }

    print("=" * 80)
    print("Testing Gemini API with Intro Options Prompt")
    print("=" * 80)
    print("\n[System Prompt]")
    print(SYSTEM_PROMPT)
    print("\n[User Prompt]")
    print(TEST_PROMPT_1)
    print("\n[Sending request to Gemini API...]")

    try:
        response = requests.post(URL, json=payload, timeout=30)
        response.raise_for_status()

        result = response.json()
        print("\n[Raw API Response]")
        print(json.dumps(result, indent=2))

        # Parse response
        if "candidates" in result and len(result["candidates"]) > 0:
            generated_text = result["candidates"][0]["content"]["parts"][0]["text"]
            print("\n[Generated Text]")
            print(generated_text)

            # Test parsing
            print("\n[Parsing Result]")
            parsed = parse_intro_options(generated_text)
            if parsed:
                print(f"✓ Welcome Message: {parsed['welcome']}")
                print(f"✓ Option 1: {parsed['option_1']}")
                print(f"✓ Option 2: {parsed['option_2']}")
                print(f"✓ Option 3: {parsed['option_3']}")
                print("\n✅ Parsing successful!")
            else:
                print("❌ Parsing failed!")
        else:
            print("❌ No candidates in response!")

    except Exception as e:
        print(f"❌ Error: {e}")


def parse_intro_options(response_text):
    """Parse the LLM response for intro options"""

    try:
        options = {}
        lines = response_text.strip().split('\n')

        for line in lines:
            line = line.strip()
            if not line:
                continue

            if line.startswith("OPTION_1|"):
                options['option_1'] = line.replace("OPTION_1|", "").strip()
            elif line.startswith("OPTION_2|"):
                options['option_2'] = line.replace("OPTION_2|", "").strip()
            elif line.startswith("OPTION_3|"):
                options['option_3'] = line.replace("OPTION_3|", "").strip()
            elif line.startswith("WELCOME|"):
                options['welcome'] = line.replace("WELCOME|", "").strip()

        # Check if all required fields are present
        if all(key in options for key in ['option_1', 'option_2', 'option_3', 'welcome']):
            return options
        else:
            print("Missing required fields:", set(['option_1', 'option_2', 'option_3', 'welcome']) - set(options.keys()))
            return None

    except Exception as e:
        print(f"Parsing error: {e}")
        return None


if __name__ == "__main__":
    test_gemini_api()
