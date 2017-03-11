## 概要

[読書メーター](https://elk.bookmeter.com/) のスクレイパーです. 

マイページの URL からユーザの読んだ本の タイトル・読了日・著者・ページ数・感想 を所得して json として出力します

## 例

macOS の場合

```
$ ./build.sh
$ mono build/dmscraper.exe
マイページのURL : https://elk.bookmeter.com/users/580549
出力先(json) : hoge.json
page 1 done!
page 2 done!
page 3 done!
page 4 done!
$ head hoge.json
[{
  "title": "羆嵐 (新潮文庫)",
  "date": [
    2017,
    3,
    8
  ],
  "author": "吉村 昭",
  "page": 226,
  "comment": "バーナード嬢を読んで以来興味があった. 自然の恐怖と共に当時の農民・開拓民の生活の価値観が感じられて良かった"
```

## 環境

F# が動けば動くはず...
