-- Drop existing dictionary and configuration if they exist
DROP TEXT SEARCH CONFIGURATION IF EXISTS english_synonyms;
DROP TEXT SEARCH DICTIONARY IF EXISTS english_synonym;

-- Create a synonym dictionary using the wordnet_synonyms.syn file
CREATE TEXT SEARCH DICTIONARY english_synonym (
    TEMPLATE = synonym,
    SYNONYMS = wordnet_synonyms
);

-- Create a text search configuration that includes synonyms
CREATE TEXT SEARCH CONFIGURATION english_synonyms (COPY = english);

-- Add the synonym dictionary to the english_synonyms configuration
ALTER TEXT SEARCH CONFIGURATION english_synonyms
    ALTER MAPPING FOR asciiword, asciihword, hword_asciipart, word, hword, hword_part
    WITH english_synonym, english_stem;

-- Example of how to test the synonym search:
-- SELECT to_tsvector('english_synonyms', 'car automobile vehicle');
-- SELECT to_tsquery('english_synonyms', 'car');
-- This should match documents containing 'automobile' or 'vehicle' as well
