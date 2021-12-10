const express = require('express');
const fs = require('fs');
const http = require('http');
const morgan = require('morgan');
const multer  = require('multer')
const path = require('path');
const bcrypt = require("bcrypt");
const dotenv = require('dotenv');
const jwt = require('jsonwebtoken');

const config = require('./config');
const { IsSubpathOf, generateAccessToken } = require('./utils');
const { users } = require('./users');

dotenv.config();

const upload = multer({ dest: config.uploadPath });
const app = express();
app.use(morgan(':remote-addr :remote-user :method :url HTTP/:http-version :user-agent :status :res[content-length] - :response-time ms'));
app.use(express.json());

function authenticateToken(req, res, next) {
  const authHeader = req.headers['authorization'];
  const token = authHeader && authHeader.split(' ')[1];

  if (token == null) {
    return res.sendStatus(401);
  }

  jwt.verify(token, process.env.TOKEN_SECRET, (err, user) => {
    if (err) {
      return res.sendStatus(403);
    }
    req.user = user;
    next();
  })
}

app.route('/manage/getToken').post(async (req, res) => {
  let auth = req.body;
  if (!auth || !auth.username || !auth.password) {
    res.status(400).send();
    return;
  }

  for (let user of users) {
    if (user.username === auth.username) {
      try {
        let result = await bcrypt.compare(auth.password, user.passwordHash);

        if (result === true) {
          console.log(`Successful login for ${auth.username}`);

          const token = generateAccessToken(auth.username);
          res.send(token);

          return;
        } else {
          console.log(`Invalid login for ${auth.username}`);
          res.status(401).send();
          return;
        }
      } catch (err) {
        console.error(err.message);
      } 
      break;
    }
  }
  res.status(400).send();
});

app.post('/manage/archive', authenticateToken, upload.single('file'), function (req, res, next) {
  if (!req.file) {
    next();
    return;
  }

  console.log(`Receiving ${req.file.originalname}`);
  const destPath = `${req.file.destination}/${req.file.originalname}`;

  if (IsSubpathOf(config.uploadPath, destPath) === true // Only in the upload path
      && path.dirname(destPath) === config.uploadPath // No subdir
    ) {
      fs.rename(req.file.path, destPath, () => {
        res.status(200).send();
      });
  } else {
    res.status(400).send();
  }
});

app.route('/manage/config').post(authenticateToken, (req, res) => {
  let newSkin = req.body;

  if (!newSkin || !newSkin.Name || !newSkin.Url || !newSkin.FileHashes) {
    res.status(400).send();
  }

  const configFilePath = `${config.uploadPath}/config.json`;
  let serverConfig = JSON.parse(fs.readFileSync(configFilePath));

  for (let i in serverConfig.Cars) {
    if (serverConfig.Cars[i].Name == 'rss_formula_hybrid_2021') {
      let found = false;
      for (let j in serverConfig.Cars[i].Skins) {
        if (serverConfig.Cars[i].Skins[j].Name == newSkin.Name) {
          found = true;
          serverConfig.Cars[i].Skins[j] = newSkin;
          break;
        }
      }

      if (!found) {
        serverConfig.Cars[i].Skins.push(newSkin);
      }
      break;
    }
  }

  fs.writeFileSync(configFilePath, JSON.stringify(serverConfig, null, 2));

  res.status(200).send();
});

const httpServer = http.createServer(app);
httpServer.listen(process.env.PORT);

console.log(`Listening on port ${process.env.PORT}`);

// To make new users
function generateNewPasswordHash() {
  const saltRounds = 12;
  const plainTextPassword1 = "password123";

  bcrypt.hash(plainTextPassword1, saltRounds).then(hash => {
    console.log(`Hash: ${hash}`);
    hash2 = hash;
    // Store hash in your password DB.
  }).catch(err => console.error(err.message));
}
