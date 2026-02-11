# Fingerprints Folder

This folder contains fingerprint images for citizens in the database.

## File Naming Convention

Fingerprint files should be named using the CitizenID from the CSV database followed by `_finger_` and the finger number (1-5).

For example:
- `CIT001_finger_1.png` - First fingerprint for John Smith (CitizenID: CIT001)
- `CIT001_finger_2.png` - Second fingerprint for John Smith
- `CIT001_finger_3.png` - Third fingerprint for John Smith
- `CIT001_finger_4.png` - Fourth fingerprint for John Smith
- `CIT001_finger_5.png` - Fifth fingerprint for John Smith

## Supported Formats

- PNG (.png)
- JPEG (.jpg, .jpeg)
- Any format supported by Unity's Sprite system

## Important Notes

1. The file name must exactly match the pattern: `{CitizenID}_finger_{number}`
2. Finger numbers range from 1 to 5
3. File extensions are case-sensitive
4. You don't need to provide all 5 fingerprints - any number from 1-5 will work
5. If no fingerprints are found for a citizen, the system will log a warning but continue functioning
6. Images will be automatically loaded as Sprites when the database initializes

## Example Structure

```
Resources/
  Fingerprints/
    CIT001_finger_1.png
    CIT001_finger_2.png
    CIT001_finger_3.png
    CIT001_finger_4.png
    CIT001_finger_5.png
    CIT002_finger_1.png
    CIT002_finger_2.png
    CIT002_finger_3.png
    ...
```

## Fingerprint Order Convention

If you want to follow standard fingerprint conventions:
1. Right thumb
2. Right index finger
3. Right middle finger
4. Right ring finger
5. Right pinky finger

However, the system doesn't enforce this - it's just a suggestion for consistency. 