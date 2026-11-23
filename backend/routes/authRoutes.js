import express from 'express';
import { 
    registerUser, 
    loginUser, 
    getProfile,
    forgotPassword,
    verifyOTP,
    resetPassword,
    getAllUsers,
    getUserById,
    updateUser,
    deleteUser } from '../controllers/authController.js';
import authMiddleware from '../middleware/authMiddleware.js';
import adminMiddleware from '../middleware/adminMiddleware.js';

const router = express.Router();

router.post('/register', registerUser);
router.post('/login', loginUser);
router.get('/profile', authMiddleware, getProfile)
router.post('/forgot-password', forgotPassword);
router.post('/verify-otp', verifyOTP);
router.post('/reset-password', resetPassword)

router.get(
    '/admin/dashboard',
    authMiddleware,
    adminMiddleware,
    (req, res) => {
        return res.status(200).json({
            success: true,
            message: 'Welcome to Admin Dashboard',
            user: req.user
        });
    }
);

router.get(
    '/admin/users',
    authMiddleware,
    adminMiddleware,
    getAllUsers
);

router.get(
    '/admin/users/:id',
    authMiddleware,
    adminMiddleware,
    getUserById
);

router.put(
    '/admin/users/:id',
    authMiddleware,
    adminMiddleware,
    updateUser
);

router.delete(
    '/admin/users/:id',
    authMiddleware,
    adminMiddleware,
    deleteUser
)

export default router;