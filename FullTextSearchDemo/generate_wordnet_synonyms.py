#!/usr/bin/env python3
import nltk
from nltk.corpus import wordnet as wn

# ดาวน์โหลด WordNet หากยังไม่มี
nltk.download('wordnet')

output_file = 'wordnet_synonyms.syn'

# เปิดไฟล์เพื่อเขียน synonyms
with open(output_file, 'w') as f:
    # ดึงคำทั้งหมดจาก WordNet
    for synset in list(wn.all_synsets()):
        # ดึงเฉพาะ synset ที่มีคำมากกว่า 1 คำ
        lemmas = synset.lemma_names()
        if len(lemmas) > 1:
            # เขียนในรูปแบบที่ PostgreSQL ต้องการ
            f.write(' '.join(lemmas) + '\n')

print(f"Synonyms written to {output_file}")