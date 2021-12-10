const path = require('path');
const jwt = require('jsonwebtoken');

module.exports = {
  IsSubpathOf(root, file) {
    const relative = path.relative(root, file);
    return relative && !relative.startsWith('..') && !path.isAbsolute(relative);
  },
  generateAccessToken(username) {
    return jwt.sign({username: username}, process.env.TOKEN_SECRET, { expiresIn: '900s' });
  },
}